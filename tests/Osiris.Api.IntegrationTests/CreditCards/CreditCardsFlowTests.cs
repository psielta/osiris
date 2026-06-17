using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Api.IntegrationTests.Support;
using Osiris.Infrastructure.Persistence;

namespace Osiris.Api.IntegrationTests.CreditCards;

public sealed record CreatedIdResponse(Guid Id);

public sealed record CardItemResponse(
    Guid Id,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    bool IsActive);

public sealed record CardDetailsResponse(
    Guid Id,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    Guid? PaymentAccountId,
    string? PaymentAccountName,
    bool IsActive);

public sealed record PurchaseItemResponse(
    Guid Id,
    string Description,
    string? CategoryName,
    decimal TotalAmount,
    DateOnly PurchaseDate,
    int Installments);

public sealed record PurchaseInstallmentResponse(
    Guid Id,
    int InstallmentNumber,
    int TotalInstallments,
    decimal Amount,
    DateOnly DueDate,
    Guid CreditCardStatementId,
    int ReferenceMonth,
    int ReferenceYear);

public sealed record PurchaseDetailsResponse(
    Guid Id,
    Guid CreditCardId,
    string CreditCardName,
    string? CategoryName,
    string Description,
    decimal TotalAmount,
    DateOnly PurchaseDate,
    int Installments,
    string? Notes,
    IReadOnlyList<PurchaseInstallmentResponse> InstallmentItems);

public sealed record StatementItemResponse(
    Guid Id,
    Guid CreditCardId,
    int ReferenceMonth,
    int ReferenceYear,
    DateOnly ClosingDate,
    DateOnly DueDate,
    int Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OpenBalance);

public sealed record StatementInstallmentResponse(
    Guid Id,
    Guid CreditCardPurchaseId,
    string PurchaseDescription,
    int InstallmentNumber,
    int TotalInstallments,
    decimal Amount);

public sealed record StatementPaymentResponse(
    Guid Id,
    decimal Amount,
    DateOnly PaidAt,
    Guid? FinancialAccountId,
    string? FinancialAccountName,
    string? Notes);

public sealed record StatementDetailsResponse(
    Guid Id,
    Guid CreditCardId,
    string CreditCardName,
    int ReferenceMonth,
    int ReferenceYear,
    DateOnly ClosingDate,
    DateOnly DueDate,
    int Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OpenBalance,
    IReadOnlyList<StatementInstallmentResponse> InstallmentItems,
    IReadOnlyList<StatementPaymentResponse> Payments);

public sealed record PreviewResponse(
    IReadOnlyList<PreviewInstallmentResponse> Installments,
    decimal TotalAmount,
    decimal Limit,
    decimal UsedLimit,
    decimal AvailableLimit,
    decimal ProjectedUsedLimit,
    decimal ProjectedUsagePercentage,
    bool HighLimitUsage,
    decimal ProjectedFutureInstallmentsTotal);

public sealed record PreviewInstallmentResponse(
    int Number,
    decimal Amount,
    int ReferenceMonth,
    int ReferenceYear,
    DateOnly DueDate);

[Collection(ApiIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CreditCardsFlowTests : IAsyncLifetime
{
    private const int Checking = 1;
    private const int ExpenseCategory = 2;

    private readonly OsirisApiApplicationFactory _factory;

    public CreditCardsFlowTests(OsirisApiApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Card_crud_archive_and_payment_account_contract()
    {
        var client = await AuthenticatedClientAsync();
        var accountId = await CreateAccountAsync(client, "Banco");

        var create = await client.PostAsJsonAsync("/api/v1/cards", new
        {
            name = "Nubank",
            limit = 1500.00m,
            closingDay = 3,
            dueDay = 10,
            paymentAccountId = accountId
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var item = Assert.Single(await ListCardsAsync(client));
        Assert.Equal(id, item.Id);
        Assert.Equal("Nubank", item.Name);
        Assert.True(item.IsActive);

        var details = await client.GetFromJsonAsync<CardDetailsResponse>($"/api/v1/cards/{id}");
        Assert.Equal(accountId, details!.PaymentAccountId);
        Assert.Equal("Banco", details.PaymentAccountName);

        var update = await client.PutAsJsonAsync($"/api/v1/cards/{id}", new
        {
            name = "Inter",
            limit = 2000.00m,
            closingDay = 5,
            dueDay = 15,
            paymentAccountId = (Guid?)null
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        details = await client.GetFromJsonAsync<CardDetailsResponse>($"/api/v1/cards/{id}");
        Assert.Equal("Inter", details!.Name);
        Assert.Null(details.PaymentAccountId);

        var archive = await client.PostAsync($"/api/v1/cards/{id}/archive", content: null);
        Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);
        Assert.Contains(await ListCardsAsync(client), card => card.Id == id && !card.IsActive);
    }

    [Fact]
    public async Task Card_payment_account_must_belong_to_same_tenant()
    {
        var clientA = await AuthenticatedClientAsync("alice@osiris.test");
        var foreignAccountId = await CreateAccountAsync(clientA, "Banco A");

        var clientB = await AuthenticatedClientAsync("bob@osiris.test");
        var response = await clientB.PostAsJsonAsync("/api/v1/cards", new
        {
            name = "Nubank",
            limit = 0m,
            closingDay = 1,
            dueDay = 1,
            paymentAccountId = foreignAccountId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListCardsAsync(clientB));
    }

    [Fact]
    public async Task Cards_and_child_resources_are_isolated_per_tenant()
    {
        var clientA = await AuthenticatedClientAsync("alice@osiris.test");
        var cardId = await CreateCardAsync(clientA, "Cartao A");
        var categoryId = await CreateExpenseCategoryAsync(clientA, "Mercado");
        var purchaseId = await CreatePurchaseAsync(clientA, cardId, categoryId, "Compra A", 80m, "2026-06-10", 1);
        var statementId = Assert.Single(await ListStatementsAsync(clientA, cardId)).Id;

        var clientB = await AuthenticatedClientAsync("bob@osiris.test");

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/cards/{cardId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/cards/{cardId}/purchases")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/cards/{cardId}/purchases/{purchaseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.DeleteAsync($"/api/v1/cards/{cardId}/purchases/{purchaseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/cards/{cardId}/statements")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/cards/{cardId}/statements/{statementId}")).StatusCode);
        Assert.DoesNotContain(await ListCardsAsync(clientB), card => card.Id == cardId);
    }

    [Fact]
    public async Task Purchase_installments_create_and_reuse_statements()
    {
        var client = await AuthenticatedClientAsync();
        var cardId = await CreateCardAsync(client, "Nubank", closingDay: 25, dueDay: 5);
        var categoryId = await CreateExpenseCategoryAsync(client, "Eletronicos");

        var response = await client.PostAsJsonAsync($"/api/v1/cards/{cardId}/purchases", new
        {
            categoryId,
            description = "Notebook",
            totalAmount = 100.00m,
            purchaseDate = "2026-06-26",
            installments = 3,
            notes = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var purchaseId = (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var purchase = await client.GetFromJsonAsync<PurchaseDetailsResponse>(
            $"/api/v1/cards/{cardId}/purchases/{purchaseId}");
        Assert.Equal(new[] { 33.33m, 33.33m, 33.34m }, purchase!.InstallmentItems.Select(i => i.Amount).ToArray());
        Assert.Equal(
            new[] { (2026, 7), (2026, 8), (2026, 9) },
            purchase.InstallmentItems.Select(i => (i.ReferenceYear, i.ReferenceMonth)).ToArray());

        await CreatePurchaseAsync(client, cardId, categoryId, "Compra 2", 50m, "2026-06-10", 1);
        await CreatePurchaseAsync(client, cardId, categoryId, "Compra 3", 70m, "2026-06-12", 1);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(4, await dbContext.CreditCardStatements.CountAsync());
        Assert.Equal(5, await dbContext.CreditCardInstallments.CountAsync());
    }

    [Fact]
    public async Task Purchase_rejects_archived_card_and_foreign_category()
    {
        var clientA = await AuthenticatedClientAsync("alice@osiris.test");
        var foreignCategoryId = await CreateExpenseCategoryAsync(clientA, "Categoria A");

        var clientB = await AuthenticatedClientAsync("bob@osiris.test");
        var cardId = await CreateCardAsync(clientB, "Cartao B");
        var response = await PostPurchaseAsync(clientB, cardId, foreignCategoryId, "Compra", 10m);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var ownCategoryId = await CreateExpenseCategoryAsync(clientB, "Mercado B");
        Assert.Equal(HttpStatusCode.NoContent, (await clientB.PostAsync($"/api/v1/cards/{cardId}/archive", content: null)).StatusCode);

        response = await PostPurchaseAsync(clientB, cardId, ownCategoryId, "Compra", 10m);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_delete_removes_installments_until_a_related_statement_is_paid()
    {
        var client = await AuthenticatedClientAsync();
        var cardId = await CreateCardAsync(client, "Nubank", closingDay: 25, dueDay: 5);
        var categoryId = await CreateExpenseCategoryAsync(client, "Mercado");

        var deletableId = await CreatePurchaseAsync(client, cardId, categoryId, "Compra deletavel", 90m, "2026-06-10", 3);
        var delete = await client.DeleteAsync($"/api/v1/cards/{cardId}/purchases/{deletableId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await dbContext.CreditCardPurchases.CountAsync());
            Assert.Equal(0, await dbContext.CreditCardInstallments.CountAsync());
        }

        var paidPurchaseId = await CreatePurchaseAsync(client, cardId, categoryId, "Compra paga", 100m, "2026-06-10", 1);
        var statement = Assert.Single(await ListStatementsAsync(client, cardId), statement => statement.OpenBalance == 100m);
        var accountId = await CreateAccountAsync(client, "Banco");
        var payment = await PayStatementAsync(client, cardId, statement.Id, 100m, accountId);
        Assert.Equal(HttpStatusCode.Created, payment.StatusCode);

        delete = await client.DeleteAsync($"/api/v1/cards/{cardId}/purchases/{paidPurchaseId}");
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await verificationContext.CreditCardPurchases.CountAsync());
        Assert.Equal(1, await verificationContext.CreditCardInstallments.CountAsync());
    }

    [Fact]
    public async Task Statements_payment_pdf_and_preview_contract()
    {
        var client = await AuthenticatedClientAsync();
        var accountId = await CreateAccountAsync(client, "Banco");
        var cardId = await CreateCardAsync(client, "Nubank", closingDay: 25, dueDay: 5, paymentAccountId: accountId);
        var categoryId = await CreateExpenseCategoryAsync(client, "Mercado");

        await CreatePurchaseAsync(client, cardId, categoryId, "Supermercado", 120m, "2026-06-20", 2);

        var preview = await client.GetFromJsonAsync<PreviewResponse>(
            $"/api/v1/cards/{cardId}/purchases/preview?totalAmount=60.00&purchaseDate=2026-06-20&installments=2");
        Assert.Equal(new[] { 30.00m, 30.00m }, preview!.Installments.Select(i => i.Amount).ToArray());

        var statements = await ListStatementsAsync(client, cardId);
        Assert.Equal(2, statements.Count);
        var firstStatement = statements.OrderBy(statement => statement.ReferenceMonth).First();

        var current = await client.GetAsync($"/api/v1/cards/{cardId}/statements/current");
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);

        var details = await client.GetFromJsonAsync<StatementDetailsResponse>(
            $"/api/v1/cards/{cardId}/statements/{firstStatement.Id}");
        Assert.Equal(60m, details!.OpenBalance);
        Assert.Single(details.InstallmentItems);

        var payment = await PayStatementAsync(client, cardId, firstStatement.Id, 30m, accountId);
        Assert.Equal(HttpStatusCode.Created, payment.StatusCode);

        details = await client.GetFromJsonAsync<StatementDetailsResponse>(
            $"/api/v1/cards/{cardId}/statements/{firstStatement.Id}");
        Assert.Equal(30m, details!.PaidAmount);
        Assert.Equal(30m, details.OpenBalance);
        Assert.Single(details.Payments);
        Assert.Equal("Banco", details.Payments.Single().FinancialAccountName);

        var pdf = await client.GetAsync($"/api/v1/cards/{cardId}/statements/{firstStatement.Id}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.True((await pdf.Content.ReadAsByteArrayAsync()).Length > 100);
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email = "owner@osiris.test")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: email);
        ApiTestHelpers.Authorize(client, tokens.AccessToken);
        return client;
    }

    private static async Task<Guid> CreateAccountAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name,
            type = Checking,
            initialBalance = 500m
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Guid> CreateExpenseCategoryAsync(HttpClient client, string name)
    {
        var uniqueName = $"{name} {Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = uniqueName,
            type = ExpenseCategory,
            color = (string?)null
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Guid> CreateCardAsync(
        HttpClient client,
        string name,
        int closingDay = 25,
        int dueDay = 5,
        Guid? paymentAccountId = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/cards", new
        {
            name,
            limit = 5000m,
            closingDay,
            dueDay,
            paymentAccountId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Guid> CreatePurchaseAsync(
        HttpClient client,
        Guid cardId,
        Guid categoryId,
        string description,
        decimal totalAmount,
        string purchaseDate,
        int installments)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/cards/{cardId}/purchases", new
        {
            categoryId,
            description,
            totalAmount,
            purchaseDate,
            installments,
            notes = (string?)null
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static Task<HttpResponseMessage> PostPurchaseAsync(
        HttpClient client,
        Guid cardId,
        Guid categoryId,
        string description,
        decimal totalAmount) =>
        client.PostAsJsonAsync($"/api/v1/cards/{cardId}/purchases", new
        {
            categoryId,
            description,
            totalAmount,
            purchaseDate = "2026-06-10",
            installments = 1,
            notes = (string?)null
        });

    private static Task<HttpResponseMessage> PayStatementAsync(
        HttpClient client,
        Guid cardId,
        Guid statementId,
        decimal amount,
        Guid? accountId) =>
        client.PostAsJsonAsync($"/api/v1/cards/{cardId}/statements/{statementId}/payments", new
        {
            amount,
            paidAt = "2026-06-30",
            financialAccountId = accountId,
            notes = (string?)null
        });

    private static async Task<List<CardItemResponse>> ListCardsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<CardItemResponse>>("/api/v1/cards"))!;

    private static async Task<List<StatementItemResponse>> ListStatementsAsync(HttpClient client, Guid cardId) =>
        (await client.GetFromJsonAsync<List<StatementItemResponse>>($"/api/v1/cards/{cardId}/statements"))!;
}
