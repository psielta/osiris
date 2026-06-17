using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.Ux;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class NavigationAndUxFlowTests : IAsyncLifetime
{
    private static readonly string[] MainRoutes =
    {
        "/dashboard",
        "/accounts",
        "/categories",
        "/cards",
        "/statements",
        "/purchases",
        "/bills",
        "/reports"
    };

    private readonly OsirisWebApplicationFactory _factory;

    public NavigationAndUxFlowTests(OsirisWebApplicationFactory factory)
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
    public async Task MainRoutes_WhenAuthenticated_ShouldLoad()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        foreach (var route in MainRoutes)
        {
            var response = await client.GetAsync(route);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Route {route} returned {(int)response.StatusCode}.");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MainRoutes_WhenAnonymous_ShouldRedirectToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        foreach (var route in MainRoutes)
        {
            var response = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith(
                "http://localhost/Account/Login",
                response.Headers.Location?.OriginalString);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Post_WithoutAntiforgeryToken_ShouldBeRejected()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await client.PostAsync(
            "/bills/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Description"] = "Sem token",
                ["Amount"] = "10.00",
                ["DueDate"] = "2026-06-10",
                ["CategoryId"] = Guid.NewGuid().ToString()
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await dbContext.Bills.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StatementsAndPurchasesOverview_WhenEmpty_ShouldShowEmptyStates()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var statementsHtml = await client.GetStringAsync("/statements");
        Assert.Contains("Nenhuma fatura ainda", statementsHtml);

        var purchasesHtml = await client.GetStringAsync("/purchases");
        Assert.Contains("Nenhuma compra registrada", purchasesHtml);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StatementsAndPurchasesOverview_WithData_ShouldListAcrossCards()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        await SeedCardWithPurchaseAsync(client, "Compra do hub", "120.00");

        var statementsHtml = await client.GetStringAsync("/statements");
        Assert.Contains("Período por vencimento", statementsHtml);
        Assert.Contains("Mês atual", statementsHtml);
        Assert.Contains("Próximo mês", statementsHtml);
        Assert.Contains("Cartao Teste", statementsHtml);
        Assert.Contains("120,00", statementsHtml);

        var purchasesHtml = await client.GetStringAsync("/purchases");
        Assert.Contains("Período por data da compra", purchasesHtml);
        Assert.Contains("Mês atual", purchasesHtml);
        Assert.Contains("Próximo mês", purchasesHtml);
        Assert.Contains("Compra do hub", purchasesHtml);
        Assert.Contains("Cartao Teste", purchasesHtml);

        var emptyStatementsHtml = await client.GetStringAsync("/statements?from=2099-01-01&to=2099-01-31");
        Assert.Contains("Nenhuma fatura ainda", emptyStatementsHtml);
        Assert.DoesNotContain("120,00", emptyStatementsHtml);

        var emptyPurchasesHtml = await client.GetStringAsync("/purchases?from=2099-01-01&to=2099-01-31");
        Assert.Contains("Nenhuma compra registrada", emptyPurchasesHtml);
        Assert.DoesNotContain("Compra do hub", emptyPurchasesHtml);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CardDetails_ShouldShowLimitOverview()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var cardId = await SeedCardWithPurchaseAsync(client, "Compra de limite", "400.00");

        var html = await client.GetStringAsync($"/cards/{cardId}");

        Assert.Contains("card-limit-overview", html);
        Assert.Contains("Limite usado", html);
        Assert.Contains("400,00", html);
        Assert.Contains("4.600,00", html);
        Assert.Contains("Total parcelado futuro", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PurchasePreview_ShouldProjectInstallmentsAndStatements()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var cardId = await CreateCardAsync(client);

        var html = await client.GetStringAsync(
            $"/cards/{cardId}/purchases/preview?totalAmount=300.00&installments=3&purchaseDate=2026-06-20");

        Assert.Contains("purchase-preview", html);
        Assert.Contains("3x de", html);
        Assert.Contains("06/2026", html);
        Assert.Contains("07/2026", html);
        Assert.Contains("08/2026", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PurchasePreview_WhenUsingMostOfTheLimit_ShouldWarn()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var cardId = await CreateCardAsync(client);

        var html = await client.GetStringAsync(
            $"/cards/{cardId}/purchases/preview?totalAmount=4500.00&installments=1&purchaseDate=2026-06-20");

        Assert.Contains("do limite usado", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Dashboard_WithOverdueStatement_ShouldShowAlert()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var cardId = await CreateCardAsync(client);
        await SeedOverdueStatementAsync(cardId);

        var html = await client.GetStringAsync("/dashboard");

        Assert.Contains("dashboard-alerts", html);
        Assert.Contains("vencida", html);
    }

    private async Task<Guid> CreateCardAsync(HttpClient client)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/cards/create");
        var response = await client.PostAsync(
            "/cards/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = "Cartao Teste",
                ["Limit"] = "5000.00",
                ["ClosingDay"] = "25",
                ["DueDay"] = "28",
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.CreditCards
            .Where(card => card.NormalizedName == CreditCard.NormalizeName("Cartao Teste"))
            .Select(card => card.Id)
            .SingleAsync();
    }

    private async Task<Guid> SeedCardWithPurchaseAsync(HttpClient client, string description, string totalAmount)
    {
        var cardId = await CreateCardAsync(client);

        var categoryId = await IntegrationTestHelpers.GetOrCreateExpenseCategoryAsync(
            _factory,
            client,
            IntegrationTestHelpers.DefaultEmail,
            "Mercado");

        var purchasePath = $"/cards/{cardId}/purchases/create";
        var purchaseToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, purchasePath);
        var response = await client.PostAsync(
            purchasePath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Description"] = description,
                ["TotalAmount"] = totalAmount,
                ["PurchaseDate"] = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToString("yyyy-MM-dd"),
                ["Installments"] = "1",
                ["CategoryId"] = categoryId.ToString(),
                ["__RequestVerificationToken"] = purchaseToken
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        return cardId;
    }

    private async Task SeedOverdueStatementAsync(Guid cardId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var card = await dbContext.CreditCards.SingleAsync(card => card.Id == cardId);
        var category = new FinancialCategory(card.TenantId, "Atrasos", Osiris.Domain.Enums.CategoryType.Expense);
        await dbContext.FinancialCategories.AddAsync(category);

        // A statement two months in the past, fully open, makes the dashboard's overdue alert fire.
        var pastMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2);
        var statement = new CreditCardStatement(
            card.TenantId,
            card.Id,
            pastMonth.Month,
            pastMonth.Year,
            new DateOnly(pastMonth.Year, pastMonth.Month, 25),
            new DateOnly(pastMonth.Year, pastMonth.Month, 28));
        await dbContext.CreditCardStatements.AddAsync(statement);

        var purchase = new CreditCardPurchase(
            card.TenantId,
            card.Id,
            category.Id,
            "Compra antiga",
            150m,
            pastMonth,
            1);
        await dbContext.CreditCardPurchases.AddAsync(purchase);

        await dbContext.CreditCardInstallments.AddAsync(new CreditCardInstallment(
            card.TenantId,
            purchase.Id,
            statement.Id,
            1,
            1,
            150m,
            statement.DueDate));

        await dbContext.SaveChangesAsync();
    }
}
