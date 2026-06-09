using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.Onboarding;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class OnboardingFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public OnboardingFlowTests(OsirisWebApplicationFactory factory)
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
    public async Task Registration_ShouldSeedDefaultCategoriesForTenant()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var tenantId = await IntegrationTestHelpers.GetTenantIdAsync(_factory, IntegrationTestHelpers.DefaultEmail);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var categories = await dbContext.FinancialCategories
            .Where(category => category.TenantId == tenantId)
            .ToArrayAsync();

        Assert.Equal(15, categories.Length);
        Assert.Equal(4, categories.Count(category => category.Type == CategoryType.Income));
        Assert.Equal(11, categories.Count(category => category.Type == CategoryType.Expense));
        Assert.Contains(categories, category => category.Name == "Moradia");
        Assert.Contains(categories, category => category.Name == "Cartão - Encargos e Juros");

        var html = await client.GetStringAsync("/categories");
        Assert.Contains("Moradia", html);
        Assert.Contains("Mercado", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Registration_ShouldSeedCategoriesPerTenantWithoutLeaking()
    {
        await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var firstTenantId = await IntegrationTestHelpers.GetTenantIdAsync(_factory, "first@osiris.test");
        var secondTenantId = await IntegrationTestHelpers.GetTenantIdAsync(_factory, "second@osiris.test");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(15, await dbContext.FinancialCategories.CountAsync(c => c.TenantId == firstTenantId));
        Assert.Equal(15, await dbContext.FinancialCategories.CountAsync(c => c.TenantId == secondTenantId));
        Assert.Equal(30, await dbContext.FinancialCategories.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Dashboard_WhenTenantHasNoFinancialData_ShouldShowOnboarding()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/dashboard");

        Assert.Contains("onboarding-card", html);
        Assert.Contains("Primeiros passos no Osiris", html);
        Assert.Contains("Criar uma conta financeira", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Dashboard_WhenSetupIsComplete_ShouldHideOnboarding()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        // Conta financeira.
        var accountToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/accounts/create");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync(
            "/accounts/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = "Banco Principal",
                ["Type"] = "1",
                ["InitialBalance"] = "1000.00",
                ["__RequestVerificationToken"] = accountToken
            }))).StatusCode);

        // Cartão de crédito.
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

        // Primeira compra usando uma categoria padrão do tenant.
        Guid cardId;
        Guid categoryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            cardId = await dbContext.CreditCards.Select(card => card.Id).SingleAsync();
            categoryId = await dbContext.FinancialCategories
                .Where(category => category.NormalizedName == FinancialCategory.NormalizeName("Mercado"))
                .Select(category => category.Id)
                .SingleAsync();
        }

        var purchasePath = $"/cards/{cardId}/purchases/create";
        var purchaseToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, purchasePath);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync(
            purchasePath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Description"] = "Primeira compra",
                ["TotalAmount"] = "50.00",
                ["PurchaseDate"] = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                ["Installments"] = "1",
                ["CategoryId"] = categoryId.ToString(),
                ["__RequestVerificationToken"] = purchaseToken
            }))).StatusCode);

        var html = await client.GetStringAsync("/dashboard");

        Assert.DoesNotContain("onboarding-card", html);
        Assert.DoesNotContain("Primeiros passos no Osiris", html);
    }
}
