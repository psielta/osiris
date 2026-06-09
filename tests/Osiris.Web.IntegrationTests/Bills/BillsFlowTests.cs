using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.Bills;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class BillsFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public BillsFlowTests(OsirisWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        return _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_WhenAnonymous_ShouldRedirectToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/bills");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(
            "http://localhost/Account/Login",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_ShouldPersistBillForOwnTenant()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var categoryId = await CreateExpenseCategoryAsync(client, "Moradia");

        var response = await PostCreateBillAsync(client, "Aluguel", "1200.00", "2026-06-10", categoryId);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bill = await dbContext.Bills.SingleAsync();
        Assert.Equal("Aluguel", bill.Description);
        Assert.Equal(1200.00m, bill.Amount);
        Assert.Equal(new DateOnly(2026, 6, 10), bill.DueDate);
        Assert.Null(bill.PaidAt);

        var user = await dbContext.Users.SingleAsync();
        Assert.Equal(user.TenantId, bill.TenantId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_ShouldListOnlyOwnTenantBills()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var firstCategory = await CreateExpenseCategoryAsync(firstClient, "Moradia", "first@osiris.test");
        var secondCategory = await CreateExpenseCategoryAsync(secondClient, "Educacao", "second@osiris.test");

        Assert.Equal(
            HttpStatusCode.Redirect,
            (await PostCreateBillAsync(firstClient, "Aluguel do primeiro", "1200.00", "2026-06-10", firstCategory)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Redirect,
            (await PostCreateBillAsync(secondClient, "Escola do segundo", "800.00", "2026-06-15", secondCategory)).StatusCode);

        var firstHtml = await firstClient.GetStringAsync("/bills?month=6&year=2026");
        Assert.Contains("Aluguel do primeiro", firstHtml);
        Assert.DoesNotContain("Escola do segundo", firstHtml);

        var secondHtml = await secondClient.GetStringAsync("/bills?month=6&year=2026");
        Assert.Contains("Escola do segundo", secondHtml);
        Assert.DoesNotContain("Aluguel do primeiro", secondHtml);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Edit_BillFromAnotherTenant_ShouldReturnNotFound()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var categoryId = await CreateExpenseCategoryAsync(firstClient, "Moradia", "first@osiris.test");
        Assert.Equal(
            HttpStatusCode.Redirect,
            (await PostCreateBillAsync(firstClient, "Aluguel", "1200.00", "2026-06-10", categoryId)).StatusCode);
        var billId = await GetSingleBillIdAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync($"/bills/{billId}/edit")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync($"/bills/{billId}")).StatusCode);

        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(secondClient, "/bills/create");
        var editResponse = await secondClient.PostAsync(
            $"/bills/{billId}/edit",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Description"] = "Invadido",
                ["Amount"] = "1.00",
                ["DueDate"] = "2026-06-10",
                ["CategoryId"] = Guid.NewGuid().ToString(),
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.NotFound, editResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bill = await dbContext.Bills.SingleAsync();
        Assert.Equal("Aluguel", bill.Description);
        Assert.Equal(1200.00m, bill.Amount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_WithCategoryFromAnotherTenant_ShouldRejectWithoutPersisting()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var foreignCategoryId = await CreateExpenseCategoryAsync(firstClient, "Moradia", "first@osiris.test");

        var response = await PostCreateBillAsync(secondClient, "Aluguel", "1200.00", "2026-06-10", foreignCategoryId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        // Razor HTML-encodes accented characters, so assert on an ASCII-only fragment.
        Assert.Contains("encontrada ou arquivada", html);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await dbContext.Bills.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Pay_ShouldMarkBillAsPaid()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var categoryId = await CreateExpenseCategoryAsync(client, "Moradia");
        Assert.Equal(
            HttpStatusCode.Redirect,
            (await PostCreateBillAsync(client, "Aluguel", "1200.00", "2026-06-10", categoryId)).StatusCode);
        var billId = await GetSingleBillIdAsync();

        var response = await PostPayAsync(client, billId, "2026-06-08");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bill = await dbContext.Bills.SingleAsync();
        Assert.Equal(new DateOnly(2026, 6, 8), bill.PaidAt);

        var html = await client.GetStringAsync($"/bills/{billId}");
        Assert.Contains("Paga", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Pay_WithAccount_ShouldReduceBalanceAndCreateBillPaymentMovement()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var categoryId = await CreateExpenseCategoryAsync(client, "Moradia");
        var accountId = await CreateAccountAsync(client, "Banco Principal", "5000.00");
        Assert.Equal(
            HttpStatusCode.Redirect,
            (await PostCreateBillAsync(client, "Aluguel", "1200.00", "2026-06-10", categoryId)).StatusCode);
        var billId = await GetSingleBillIdAsync();

        var response = await PostPayAsync(client, billId, "2026-06-08", accountId);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var account = await dbContext.FinancialAccounts.SingleAsync(account => account.Id == accountId);
        Assert.Equal(3800.00m, account.CurrentBalance);

        // One uncategorized BillPayment movement: paying a bill never duplicates the expense.
        var movement = await dbContext.FinancialAccountMovements.SingleAsync();
        Assert.Equal(FinancialAccountMovementType.BillPayment, movement.Type);
        Assert.Equal(1200.00m, movement.Amount);
        Assert.Null(movement.CategoryId);
        Assert.Equal(nameof(Bill), movement.RelatedEntityType);
        Assert.Equal(billId, movement.RelatedEntityId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Pending_AfterPaidFromAccount_ShouldRemoveMovementAndRestoreBalance()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var categoryId = await CreateExpenseCategoryAsync(client, "Moradia");
        var accountId = await CreateAccountAsync(client, "Banco Principal", "5000.00");
        Assert.Equal(
            HttpStatusCode.Redirect,
            (await PostCreateBillAsync(client, "Aluguel", "1200.00", "2026-06-10", categoryId)).StatusCode);
        var billId = await GetSingleBillIdAsync();
        Assert.Equal(HttpStatusCode.Redirect, (await PostPayAsync(client, billId, "2026-06-08", accountId)).StatusCode);

        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/bills/{billId}");
        var response = await client.PostAsync(
            $"/bills/{billId}/pending",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var bill = await dbContext.Bills.SingleAsync();
        Assert.Null(bill.PaidAt);

        Assert.Equal(0, await dbContext.FinancialAccountMovements.CountAsync());

        var account = await dbContext.FinancialAccounts.SingleAsync(account => account.Id == accountId);
        Assert.Equal(5000.00m, account.CurrentBalance);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_MonthYearFilter_ShouldShowOnlyMatchingBills()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var categoryId = await CreateExpenseCategoryAsync(client, "Moradia");

        Assert.Equal(
            HttpStatusCode.Redirect,
            (await PostCreateBillAsync(client, "Conta de junho", "100.00", "2026-06-10", categoryId)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Redirect,
            (await PostCreateBillAsync(client, "Conta de julho", "100.00", "2026-07-10", categoryId)).StatusCode);

        var juneHtml = await client.GetStringAsync("/bills?month=6&year=2026");
        Assert.Contains("Conta de junho", juneHtml);
        Assert.DoesNotContain("Conta de julho", juneHtml);

        var julyHtml = await client.GetStringAsync("/bills?month=7&year=2026");
        Assert.Contains("Conta de julho", julyHtml);
        Assert.DoesNotContain("Conta de junho", julyHtml);
    }

    private Task<Guid> CreateExpenseCategoryAsync(
        HttpClient client,
        string name,
        string email = IntegrationTestHelpers.DefaultEmail)
    {
        return IntegrationTestHelpers.GetOrCreateExpenseCategoryAsync(_factory, client, email, name);
    }

    private async Task<Guid> CreateAccountAsync(HttpClient client, string name, string initialBalance)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/accounts/create");
        var response = await client.PostAsync(
            "/accounts/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Type"] = "1",
                ["InitialBalance"] = initialBalance,
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.FinancialAccounts
            .Where(account => account.NormalizedName == FinancialAccount.NormalizeName(name))
            .Select(account => account.Id)
            .SingleAsync();
    }

    private static async Task<HttpResponseMessage> PostCreateBillAsync(
        HttpClient client,
        string description,
        string amount,
        string dueDate,
        Guid categoryId,
        Guid? paymentAccountId = null)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/bills/create");
        var values = new Dictionary<string, string>
        {
            ["Description"] = description,
            ["Amount"] = amount,
            ["DueDate"] = dueDate,
            ["CategoryId"] = categoryId.ToString(),
            ["__RequestVerificationToken"] = token
        };

        if (paymentAccountId is not null)
        {
            values["PaymentAccountId"] = paymentAccountId.Value.ToString();
        }

        return await client.PostAsync("/bills/create", new FormUrlEncodedContent(values));
    }

    private static async Task<HttpResponseMessage> PostPayAsync(
        HttpClient client,
        Guid billId,
        string paidAt,
        Guid? accountId = null)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/bills/{billId}");
        var values = new Dictionary<string, string>
        {
            ["PaidAt"] = paidAt,
            ["__RequestVerificationToken"] = token
        };

        if (accountId is not null)
        {
            values["PaymentAccountId"] = accountId.Value.ToString();
        }

        return await client.PostAsync($"/bills/{billId}/pay", new FormUrlEncodedContent(values));
    }

    private async Task<Guid> GetSingleBillIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Bills.Select(bill => bill.Id).SingleAsync();
    }
}
