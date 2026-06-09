using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.Dashboard;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class DashboardFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public DashboardFlowTests(OsirisWebApplicationFactory factory)
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

    private static DateOnly BrazilToday()
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_WhenAnonymous_ShouldRedirectToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(
            "http://localhost/Account/Login",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_DefaultMonth_ShouldShowCurrentMonthObligations()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var categoryId = await CreateExpenseCategoryAsync(client, "Moradia");
        var today = BrazilToday();
        await CreateBillAsync(client, "Aluguel do mes corrente", "1200.00", today.ToString("yyyy-MM-dd"), categoryId);

        var html = await client.GetStringAsync("/dashboard");

        Assert.Contains("Aluguel do mes corrente", html);
        Assert.Contains("1.200,00", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_MonthFilter_ShouldHideOtherMonths()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var categoryId = await CreateExpenseCategoryAsync(client, "Moradia");
        var today = BrazilToday();
        await CreateBillAsync(client, "Conta deste mes", "150.00", today.ToString("yyyy-MM-dd"), categoryId);

        var otherMonth = today.AddMonths(3);
        var filteredHtml = await client.GetStringAsync($"/dashboard?month={otherMonth.Month}&year={otherMonth.Year}");
        Assert.DoesNotContain("Conta deste mes", filteredHtml);

        var currentHtml = await client.GetStringAsync($"/dashboard?month={today.Month}&year={today.Year}");
        Assert.Contains("Conta deste mes", currentHtml);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_CardPurchase_ShouldAppearInSpendingByCategory()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var context = await SeedCardWithPurchaseAsync(client, "317.00");

        var today = BrazilToday();
        var html = await client.GetStringAsync($"/dashboard?month={today.Month}&year={today.Year}");

        Assert.Contains("spending-by-category", html);
        Assert.Contains("Mercado", html);
        Assert.Contains("317,00", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_StatementPayment_ShouldShowInCashWithoutDuplicatingSpending()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var context = await SeedCardWithPurchaseAsync(client, "100.00");
        var today = BrazilToday();

        var payPath = $"/cards/{context.CardId}/statements/{context.StatementId}/pay";
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, payPath);
        var payResponse = await client.PostAsync(
            payPath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Payment.Amount"] = "40.00",
                ["Payment.PaidAt"] = today.ToString("yyyy-MM-dd"),
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.Redirect, payResponse.StatusCode);

        var html = await client.GetStringAsync($"/dashboard?month={today.Month}&year={today.Year}");

        // The purchase appears once as spending; the payment shows in the cash view.
        Assert.Contains("100,00", html);
        Assert.Contains("40,00", html);
        Assert.DoesNotContain("140,00", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_ShouldNotShowDataFromAnotherTenant()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var categoryId = await CreateExpenseCategoryAsync(firstClient, "Moradia");
        var today = BrazilToday();
        await CreateBillAsync(firstClient, "Aluguel exclusivo do primeiro", "1200.00", today.ToString("yyyy-MM-dd"), categoryId);
        await SeedCardWithPurchaseAsync(firstClient, "250.00");

        var firstHtml = await firstClient.GetStringAsync($"/dashboard?month={today.Month}&year={today.Year}");
        Assert.Contains("Aluguel exclusivo do primeiro", firstHtml);
        Assert.Contains("Cartao Teste", firstHtml);

        // The second tenant's dashboard shows none of it: no bill, no card, no statement.
        var secondHtml = await secondClient.GetStringAsync($"/dashboard?month={today.Month}&year={today.Year}");
        Assert.DoesNotContain("Aluguel exclusivo do primeiro", secondHtml);
        Assert.DoesNotContain("Cartao Teste", secondHtml);
        Assert.DoesNotContain("250,00", secondHtml);
    }

    private sealed record SeededCardPurchase(Guid CardId, Guid StatementId);

    private async Task<SeededCardPurchase> SeedCardWithPurchaseAsync(HttpClient client, string totalAmount)
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

        var categoryId = await CreateExpenseCategoryAsync(client, "Mercado");

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
                ["Description"] = "Compra do painel",
                ["TotalAmount"] = totalAmount,
                ["PurchaseDate"] = BrazilToday().ToString("yyyy-MM-dd"),
                ["Installments"] = "1",
                ["CategoryId"] = categoryId.ToString(),
                ["__RequestVerificationToken"] = purchaseToken
            }));
        Assert.Equal(HttpStatusCode.Redirect, purchaseResponse.StatusCode);

        using var statementScope = _factory.Services.CreateScope();
        var statementContext = statementScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var statementId = await statementContext.CreditCardStatements
            .Where(statement => statement.CreditCardId == cardId)
            .Select(statement => statement.Id)
            .SingleAsync();

        return new SeededCardPurchase(cardId, statementId);
    }

    private async Task<Guid> CreateExpenseCategoryAsync(HttpClient client, string name)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/categories/create");
        var response = await client.PostAsync(
            "/categories/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Type"] = "2",
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.FinancialCategories
            .Where(category => category.NormalizedName == FinancialCategory.NormalizeName(name))
            .Select(category => category.Id)
            .SingleAsync();
    }

    private static async Task CreateBillAsync(
        HttpClient client,
        string description,
        string amount,
        string dueDate,
        Guid categoryId)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/bills/create");
        var response = await client.PostAsync(
            "/bills/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Description"] = description,
                ["Amount"] = amount,
                ["DueDate"] = dueDate,
                ["CategoryId"] = categoryId.ToString(),
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
