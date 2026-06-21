using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Api.IntegrationTests.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Persistence;

namespace Osiris.Api.IntegrationTests.Bills;

public sealed record CreatedIdResponse(Guid Id);

public sealed record BillItemResponse(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly DueDate,
    DateOnly? PaidAt,
    int Status,
    Guid CategoryId,
    string? CategoryName,
    string? CategoryColor,
    Guid? PaymentAccountId,
    string? PaymentAccountName);

public sealed record BillDetailsResponse(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly DueDate,
    DateOnly? PaidAt,
    int Status,
    Guid CategoryId,
    string? CategoryName,
    string? CategoryColor,
    Guid? PaymentAccountId,
    string? PaymentAccountName,
    string? Notes);

public sealed record DashboardResponse(
    int Year,
    int Month,
    decimal IncomeTotal,
    decimal SpendingTotal,
    decimal TotalOpenStatementsBalance,
    decimal TotalOpenBillsBalance,
    decimal FutureInstallmentsTotal,
    IReadOnlyList<SpendingCategoryResponse> SpendingByCategory);

public sealed record SpendingCategoryResponse(
    Guid? CategoryId,
    string CategoryName,
    decimal CardPurchasesTotal,
    decimal BillsTotal,
    decimal DirectExpensesTotal);

[Collection(ApiIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class BillsFlowTests : IAsyncLifetime
{
    private const int Checking = 1;
    private const int ExpenseCategory = 2;

    private readonly OsirisApiApplicationFactory _factory;

    public BillsFlowTests(OsirisApiApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Bill_crud_payment_pending_and_delete_contract()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = await CreateExpenseCategoryAsync(client, "Aluguel");
        var accountId = await CreateAccountAsync(client, "Banco", 500m);

        var create = await client.PostAsJsonAsync("/api/v1/bills", new
        {
            description = "Aluguel",
            amount = 120.00m,
            dueDate = "2026-06-30",
            categoryId,
            paymentAccountId = accountId,
            notes = "Contrato"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        Assert.Contains(await ListBillsAsync(client, 6, 2026), bill => bill.Id == id && bill.Status == (int)BillStatus.Pending);

        var update = await client.PutAsJsonAsync($"/api/v1/bills/{id}", new
        {
            description = "Aluguel residencial",
            amount = 150.00m,
            dueDate = "2026-06-25",
            categoryId,
            paymentAccountId = accountId,
            notes = "Atualizado"
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var details = await client.GetFromJsonAsync<BillDetailsResponse>($"/api/v1/bills/{id}");
        Assert.Equal("Aluguel residencial", details!.Description);
        Assert.Equal(150m, details.Amount);

        var pay = await client.PostAsJsonAsync($"/api/v1/bills/{id}/pay", new
        {
            paidAt = "2026-06-25",
            paymentAccountId = accountId
        });
        Assert.Equal(HttpStatusCode.NoContent, pay.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await dbContext.FinancialAccounts.SingleAsync(account => account.Id == accountId);
            Assert.Equal(350m, account.CurrentBalance);

            var movement = await dbContext.FinancialAccountMovements.SingleAsync();
            Assert.Equal(FinancialAccountMovementType.BillPayment, movement.Type);
            Assert.Null(movement.CategoryId);
            Assert.Equal(nameof(Bill), movement.RelatedEntityType);
            Assert.Equal(id, movement.RelatedEntityId);
        }

        var pending = await client.PostAsync($"/api/v1/bills/{id}/pending", content: null);
        Assert.Equal(HttpStatusCode.NoContent, pending.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await dbContext.FinancialAccounts.SingleAsync(account => account.Id == accountId);
            Assert.Equal(500m, account.CurrentBalance);
            Assert.Equal(0, await dbContext.FinancialAccountMovements.CountAsync());
        }

        var delete = await client.DeleteAsync($"/api/v1/bills/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.DoesNotContain(await ListBillsAsync(client, 6, 2026), bill => bill.Id == id);
    }

    [Fact]
    public async Task Bill_category_and_payment_account_must_belong_to_same_tenant()
    {
        var clientA = await AuthenticatedClientAsync("alice@osiris.test");
        var foreignCategoryId = await CreateExpenseCategoryAsync(clientA, "Categoria A");
        var foreignAccountId = await CreateAccountAsync(clientA, "Conta A", 100m);

        var clientB = await AuthenticatedClientAsync("bob@osiris.test");
        var ownCategoryId = await CreateExpenseCategoryAsync(clientB, "Categoria B");

        var foreignCategory = await PostBillAsync(clientB, foreignCategoryId, null);
        Assert.Equal(HttpStatusCode.BadRequest, foreignCategory.StatusCode);

        var foreignAccount = await PostBillAsync(clientB, ownCategoryId, foreignAccountId);
        Assert.Equal(HttpStatusCode.BadRequest, foreignAccount.StatusCode);

        Assert.Empty(await ListBillsAsync(clientB, 6, 2026));
    }

    [Fact]
    public async Task Bills_are_isolated_per_tenant()
    {
        var clientA = await AuthenticatedClientAsync("alice@osiris.test");
        var categoryA = await CreateExpenseCategoryAsync(clientA, "Categoria A");
        var billId = await CreateBillAsync(clientA, categoryA);

        var clientB = await AuthenticatedClientAsync("bob@osiris.test");
        var categoryB = await CreateExpenseCategoryAsync(clientB, "Categoria B");
        var accountB = await CreateAccountAsync(clientB, "Conta B", 100m);

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/bills/{billId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PutAsJsonAsync($"/api/v1/bills/{billId}", new
        {
            description = "Invasao",
            amount = 10m,
            dueDate = "2026-06-20",
            categoryId = categoryB,
            paymentAccountId = (Guid?)null,
            notes = (string?)null
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PostAsJsonAsync($"/api/v1/bills/{billId}/pay", new
        {
            paidAt = "2026-06-20",
            paymentAccountId = accountB
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PostAsync($"/api/v1/bills/{billId}/pending", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.DeleteAsync($"/api/v1/bills/{billId}")).StatusCode);
    }

    [Fact]
    public async Task Dashboard_endpoint_returns_monthly_spending_and_open_obligations()
    {
        var client = await AuthenticatedClientAsync();
        var accountId = await CreateAccountAsync(client, "Banco", 500m);
        var categoryId = await CreateExpenseCategoryAsync(client, "Casa");
        var cardId = await CreateCardAsync(client, "Nubank");

        await CreatePurchaseAsync(client, cardId, categoryId, "Mercado", 100m);
        await CreateBillAsync(client, categoryId, "Internet", 50m);

        var dashboard = await client.GetFromJsonAsync<DashboardResponse>("/api/v1/dashboard?month=6&year=2026");

        Assert.Equal(2026, dashboard!.Year);
        Assert.Equal(6, dashboard.Month);
        Assert.Equal(150m, dashboard.SpendingTotal);
        Assert.Equal(100m, dashboard.TotalOpenStatementsBalance);
        Assert.Equal(50m, dashboard.TotalOpenBillsBalance);

        var category = Assert.Single(dashboard.SpendingByCategory, item => item.CategoryId == categoryId);
        Assert.Equal(100m, category.CardPurchasesTotal);
        Assert.Equal(50m, category.BillsTotal);
        Assert.Equal(0m, category.DirectExpensesTotal);
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email = "owner@osiris.test")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: email);
        ApiTestHelpers.Authorize(client, tokens.AccessToken);
        return client;
    }

    private static async Task<Guid> CreateAccountAsync(HttpClient client, string name, decimal initialBalance)
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name,
            type = Checking,
            initialBalance
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Guid> CreateExpenseCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = $"{name} {Guid.NewGuid():N}",
            type = ExpenseCategory,
            color = (string?)null
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Guid> CreateCardAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/cards", new
        {
            name,
            limit = 2000m,
            closingDay = 25,
            dueDay = 5,
            paymentAccountId = (Guid?)null
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Guid> CreatePurchaseAsync(
        HttpClient client,
        Guid cardId,
        Guid categoryId,
        string description,
        decimal totalAmount)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/cards/{cardId}/purchases", new
        {
            categoryId,
            description,
            totalAmount,
            purchaseDate = "2026-06-10",
            installments = 1,
            notes = (string?)null
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Guid> CreateBillAsync(
        HttpClient client,
        Guid categoryId,
        string description = "Aluguel",
        decimal amount = 120m)
    {
        var response = await PostBillAsync(client, categoryId, null, description, amount);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static Task<HttpResponseMessage> PostBillAsync(
        HttpClient client,
        Guid categoryId,
        Guid? paymentAccountId,
        string description = "Conta",
        decimal amount = 100m) =>
        client.PostAsJsonAsync("/api/v1/bills", new
        {
            description,
            amount,
            dueDate = "2026-06-20",
            categoryId,
            paymentAccountId,
            notes = (string?)null
        });

    private static async Task<List<BillItemResponse>> ListBillsAsync(HttpClient client, int month, int year) =>
        (await client.GetFromJsonAsync<List<BillItemResponse>>($"/api/v1/bills?month={month}&year={year}"))!;
}
