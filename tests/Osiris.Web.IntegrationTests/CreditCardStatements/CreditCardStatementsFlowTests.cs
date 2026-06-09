using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.CreditCardStatements;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class CreditCardStatementsFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public CreditCardStatementsFlowTests(OsirisWebApplicationFactory factory)
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

        var response = await client.GetAsync($"/cards/{Guid.NewGuid()}/statements");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(
            "http://localhost/Account/Login",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Details_ShouldShowInstallmentsAndTotals()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var context = await SeedPurchaseAsync(client, totalAmount: "90.00", installments: "3");

        var html = await client.GetStringAsync($"/cards/{context.CardId}/statements/{context.StatementId}");

        Assert.Contains("Compra integrada", html);
        Assert.Contains("1 de 3", html);
        Assert.Contains("Pagar valor total", html);
        Assert.Contains("Registrar pagamento parcial", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Pay_PartialPayment_ShouldPersistPaymentAndMarkPartiallyPaid()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var context = await SeedPurchaseAsync(client, totalAmount: "100.00", installments: "1");

        var response = await PostPaymentAsync(client, context.CardId, context.StatementId, "40.00");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var payment = await dbContext.CreditCardStatementPayments.SingleAsync();
        Assert.Equal(40.00m, payment.Amount);

        var statement = await dbContext.CreditCardStatements.SingleAsync();
        Assert.Equal(CreditCardStatementStatus.PartiallyPaid, statement.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Pay_FullPayment_ShouldMarkStatementAsPaid()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var context = await SeedPurchaseAsync(client, totalAmount: "100.00", installments: "1");

        var response = await PostPaymentAsync(client, context.CardId, context.StatementId, "100.00");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var statement = await dbContext.CreditCardStatements.SingleAsync();
        Assert.Equal(CreditCardStatementStatus.Paid, statement.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Pay_WithAccount_ShouldReduceBalanceAndCreateUncategorizedMovement()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var context = await SeedPurchaseAsync(client, totalAmount: "100.00", installments: "1");
        var accountId = await CreateAccountAsync(client, "Banco Principal", initialBalance: "500.00");

        var response = await PostPaymentAsync(client, context.CardId, context.StatementId, "100.00", accountId);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var account = await dbContext.FinancialAccounts.SingleAsync(account => account.Id == accountId);
        Assert.Equal(400.00m, account.CurrentBalance);

        // Exactly one movement: the cash outflow of the payment, never a categorized expense.
        var movement = await dbContext.FinancialAccountMovements.SingleAsync();
        Assert.Equal(FinancialAccountMovementType.CreditCardStatementPayment, movement.Type);
        Assert.Equal(100.00m, movement.Amount);
        Assert.Null(movement.CategoryId);
        Assert.Equal(accountId, movement.FinancialAccountId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Pay_AmountAboveOpenBalance_ShouldRejectWithoutPersisting()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var context = await SeedPurchaseAsync(client, totalAmount: "100.00", installments: "1");

        var response = await PostPaymentAsync(client, context.CardId, context.StatementId, "150.00");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await dbContext.CreditCardStatementPayments.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task User_ShouldNotAccessOrPayStatementFromAnotherTenant()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var context = await SeedPurchaseAsync(firstClient, totalAmount: "100.00", installments: "1", email: "first@osiris.test");

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await secondClient.GetAsync($"/cards/{context.CardId}/statements")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await secondClient.GetAsync($"/cards/{context.CardId}/statements/{context.StatementId}")).StatusCode);

        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(secondClient, "/cards");
        var payResponse = await secondClient.PostAsync(
            $"/cards/{context.CardId}/statements/{context.StatementId}/pay",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Payment.Amount"] = "100.00",
                ["Payment.PaidAt"] = "2026-06-28",
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.NotFound, payResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await dbContext.CreditCardStatementPayments.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Pay_WithAccountFromAnotherTenant_ShouldRejectWithoutPersisting()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var foreignAccountId = await CreateAccountAsync(firstClient, "Banco do A", initialBalance: "500.00");
        var context = await SeedPurchaseAsync(secondClient, totalAmount: "100.00", installments: "1", email: "second@osiris.test");

        var response = await PostPaymentAsync(
            secondClient,
            context.CardId,
            context.StatementId,
            "100.00",
            foreignAccountId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await dbContext.CreditCardStatementPayments.CountAsync());

        var account = await dbContext.FinancialAccounts.SingleAsync(account => account.Id == foreignAccountId);
        Assert.Equal(500.00m, account.CurrentBalance);
    }

    private sealed record SeededPurchase(Guid CardId, Guid StatementId);

    private async Task<SeededPurchase> SeedPurchaseAsync(
        HttpClient client,
        string totalAmount,
        string installments,
        string email = IntegrationTestHelpers.DefaultEmail)
    {
        var cardToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/cards/create");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync(
            "/cards/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = "Cartao Teste",
                ["Limit"] = "5000.00",
                ["ClosingDay"] = "25",
                ["DueDay"] = "5",
                ["__RequestVerificationToken"] = cardToken
            }))).StatusCode);

        var categoryId = await IntegrationTestHelpers.GetOrCreateExpenseCategoryAsync(
            _factory,
            client,
            email,
            "Mercado");

        Guid cardId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            cardId = await dbContext.CreditCards
                .Where(card => card.NormalizedName == CreditCard.NormalizeName("Cartao Teste"))
                .Select(card => card.Id)
                .SingleAsync();
        }

        var purchasePath = $"/cards/{cardId}/purchases/create";
        var purchaseToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, purchasePath);
        var purchaseResponse = await client.PostAsync(
            purchasePath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Description"] = "Compra integrada",
                ["TotalAmount"] = totalAmount,
                ["PurchaseDate"] = "2026-06-20",
                ["Installments"] = installments,
                ["CategoryId"] = categoryId.ToString(),
                ["__RequestVerificationToken"] = purchaseToken
            }));
        Assert.Equal(HttpStatusCode.Redirect, purchaseResponse.StatusCode);

        using var statementScope = _factory.Services.CreateScope();
        var statementContext = statementScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var statementId = await statementContext.CreditCardStatements
            .Where(statement => statement.CreditCardId == cardId
                && statement.ReferenceYear == 2026
                && statement.ReferenceMonth == 6)
            .Select(statement => statement.Id)
            .SingleAsync();

        return new SeededPurchase(cardId, statementId);
    }

    private static async Task<HttpResponseMessage> PostPaymentAsync(
        HttpClient client,
        Guid cardId,
        Guid statementId,
        string amount,
        Guid? accountId = null)
    {
        var payPath = $"/cards/{cardId}/statements/{statementId}/pay";
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, payPath);
        var values = new Dictionary<string, string>
        {
            ["Payment.Amount"] = amount,
            ["Payment.PaidAt"] = "2026-06-28",
            ["__RequestVerificationToken"] = token
        };

        if (accountId is not null)
        {
            values["Payment.FinancialAccountId"] = accountId.Value.ToString();
        }

        return await client.PostAsync(payPath, new FormUrlEncodedContent(values));
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
        var normalizedName = FinancialAccount.NormalizeName(name);
        return await dbContext.FinancialAccounts
            .Where(account => account.NormalizedName == normalizedName)
            .Select(account => account.Id)
            .SingleAsync();
    }
}
