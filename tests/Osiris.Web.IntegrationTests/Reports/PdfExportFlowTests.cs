using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.Reports;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class PdfExportFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public PdfExportFlowTests(OsirisWebApplicationFactory factory)
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
    public async Task Reports_Index_ShouldShowCashFlowReports()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/reports");

        Assert.Contains("Relat", html);
        Assert.Contains("Vis", html);
        Assert.Contains("caixa", html);
        Assert.Contains("/reports/cash-flow/synthetic/pdf", html);
        Assert.Contains("/reports/cash-flow/analytic/pdf", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Reports_Index_WhenAnonymous_ShouldRedirectToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/reports");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("http://localhost/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExportPdf_CashFlowSynthetic_ShouldReturnPdfDocument()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await client.GetAsync("/reports/cash-flow/synthetic/pdf?month=6&year=2026");

        await AssertPdfAttachmentAsync(response);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExportPdf_CashFlowAnalytic_ShouldReturnPdfDocument()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await client.GetAsync("/reports/cash-flow/analytic/pdf?month=6&year=2026");

        await AssertPdfAttachmentAsync(response);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExportPdf_AccountStatement_ShouldReturnPdfDocument()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var accountId = await CreateAccountAsync(client, "Conta Corrente");
        await CreateIncomeMovementAsync(client, accountId, amount: "250.00", description: "Salário");

        var response = await client.GetAsync($"/accounts/{accountId}/pdf");

        await AssertPdfAttachmentAsync(response);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExportPdf_AccountStatement_WhenUnknownId_ShouldReturnNotFound()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await client.GetAsync($"/accounts/{Guid.NewGuid()}/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExportPdf_AccountStatement_WhenAnonymous_ShouldRedirectToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/accounts/{Guid.NewGuid()}/pdf");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("http://localhost/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExportPdf_CreditCardStatement_ShouldReturnPdfDocument()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var (cardId, statementId) = await SeedStatementAsync(client);

        var response = await client.GetAsync($"/cards/{cardId}/statements/{statementId}/pdf");

        await AssertPdfAttachmentAsync(response);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExportPdf_CreditCardStatement_FromAnotherTenant_ShouldReturnNotFound()
    {
        var owner = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var intruder = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var (cardId, statementId) = await SeedStatementAsync(owner, email: "first@osiris.test");

        var response = await intruder.GetAsync($"/cards/{cardId}/statements/{statementId}/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task AssertPdfAttachmentAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private async Task<Guid> CreateAccountAsync(HttpClient client, string name)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/accounts/create");
        var response = await client.PostAsync(
            "/accounts/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Type"] = "1",
                ["InitialBalance"] = "1000.00",
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

    private static async Task CreateIncomeMovementAsync(HttpClient client, Guid accountId, string amount, string description)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/accounts/{accountId}");
        var response = await client.PostAsync(
            $"/accounts/{accountId}/movements",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Movement.Type"] = "1", // Income
                ["Movement.Amount"] = amount,
                ["Movement.OccurredOn"] = "2026-06-10",
                ["Movement.Description"] = description,
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private async Task<(Guid CardId, Guid StatementId)> SeedStatementAsync(
        HttpClient client,
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

        var categoryId = await IntegrationTestHelpers.GetOrCreateExpenseCategoryAsync(_factory, client, email, "Mercado");

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
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync(
            purchasePath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Description"] = "Compra integrada",
                ["TotalAmount"] = "90.00",
                ["PurchaseDate"] = "2026-06-20",
                ["Installments"] = "3",
                ["CategoryId"] = categoryId.ToString(),
                ["__RequestVerificationToken"] = purchaseToken
            }))).StatusCode);

        using var statementScope = _factory.Services.CreateScope();
        var statementContext = statementScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var statementId = await statementContext.CreditCardStatements
            .Where(statement => statement.CreditCardId == cardId
                && statement.ReferenceYear == 2026
                && statement.ReferenceMonth == 6)
            .Select(statement => statement.Id)
            .SingleAsync();

        return (cardId, statementId);
    }
}
