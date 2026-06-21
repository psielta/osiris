using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.CreditCardPurchases;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class CreditCardPurchasesFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public CreditCardPurchasesFlowTests(OsirisWebApplicationFactory factory)
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

        var response = await client.GetAsync($"/cards/{Guid.NewGuid()}/purchases");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(
            "http://localhost/Account/Login",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_CashPurchase_ShouldCreateInstallmentAndStatementWithoutCashMovement()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var cardId = await CreateCardAsync(client, "Nubank", closingDay: 25, dueDay: 5);
        var categoryId = await CreateExpenseCategoryAsync(client, "Mercado");

        var response = await PostPurchaseAsync(
            client,
            cardId,
            categoryId,
            "Compras do mês",
            "100.00",
            "2026-06-20",
            installments: "1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var purchase = await dbContext.CreditCardPurchases.SingleAsync();
        Assert.Equal("Compras do mês", purchase.Description);
        Assert.Equal(100.00m, purchase.TotalAmount);
        Assert.Equal(1, purchase.Installments);
        Assert.Equal(categoryId, purchase.CategoryId);

        var installment = await dbContext.CreditCardInstallments.SingleAsync();
        Assert.Equal(100.00m, installment.Amount);

        var statement = await dbContext.CreditCardStatements.SingleAsync();
        Assert.Equal(6, statement.ReferenceMonth);
        Assert.Equal(2026, statement.ReferenceYear);
        Assert.Equal(new DateOnly(2026, 6, 25), statement.ClosingDate);
        Assert.Equal(new DateOnly(2026, 7, 5), statement.DueDate);
        Assert.Equal(statement.Id, installment.CreditCardStatementId);

        // A card purchase is debt, not cash: no account movement may exist at purchase time.
        Assert.Equal(0, await dbContext.FinancialAccountMovements.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_InstalledPurchase_ShouldCreateStatementsAutomaticallyAndSplitAmounts()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var cardId = await CreateCardAsync(client, "Nubank", closingDay: 25, dueDay: 5);
        var categoryId = await CreateExpenseCategoryAsync(client, "Eletrônicos");

        // Purchase after the closing day: first installment goes to the July statement.
        var response = await PostPurchaseAsync(
            client,
            cardId,
            categoryId,
            "Notebook",
            "100.00",
            "2026-06-26",
            installments: "3");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var installments = await dbContext.CreditCardInstallments
            .OrderBy(installment => installment.InstallmentNumber)
            .ToArrayAsync();
        Assert.Equal(3, installments.Length);
        Assert.Equal(new[] { 33.33m, 33.33m, 33.34m }, installments.Select(installment => installment.Amount).ToArray());
        Assert.Equal(100.00m, installments.Sum(installment => installment.Amount));

        var statements = await dbContext.CreditCardStatements
            .OrderBy(statement => statement.ReferenceYear)
            .ThenBy(statement => statement.ReferenceMonth)
            .ToArrayAsync();
        Assert.Equal(3, statements.Length);
        Assert.Equal(
            new[] { (2026, 7), (2026, 8), (2026, 9) },
            statements.Select(statement => (statement.ReferenceYear, statement.ReferenceMonth)).ToArray());

        // Each installment landed in its own statement, in order.
        for (var index = 0; index < installments.Length; index++)
        {
            Assert.Equal(statements[index].Id, installments[index].CreditCardStatementId);
            Assert.Equal(statements[index].DueDate, installments[index].DueDate);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_PerInstallmentAmount_ShouldMultiplyToTotal()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var cardId = await CreateCardAsync(client, "Nubank", closingDay: 25, dueDay: 5);
        var categoryId = await CreateExpenseCategoryAsync(client, "Eletrônicos");

        var path = $"/cards/{cardId}/purchases/create";
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, path);

        // The user types the per-installment value (50.00) and 3 parcelas; the form must compute total 150.00.
        var response = await client.PostAsync(
            path,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Description"] = "Geladeira",
                ["AmountMode"] = "PerInstallment",
                ["TotalAmount"] = "50.00",
                ["PurchaseDate"] = "2026-06-10",
                ["Installments"] = "3",
                ["CategoryId"] = categoryId.ToString(),
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var purchase = await dbContext.CreditCardPurchases.SingleAsync();
        Assert.Equal(150.00m, purchase.TotalAmount);
        Assert.Equal(3, purchase.Installments);

        var amounts = await dbContext.CreditCardInstallments
            .OrderBy(installment => installment.InstallmentNumber)
            .Select(installment => installment.Amount)
            .ToArrayAsync();
        Assert.Equal(new[] { 50.00m, 50.00m, 50.00m }, amounts);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_TwoPurchasesInSameCycle_ShouldReuseStatement()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var cardId = await CreateCardAsync(client, "Nubank", closingDay: 25, dueDay: 5);
        var categoryId = await CreateExpenseCategoryAsync(client, "Mercado");

        await PostPurchaseAsync(client, cardId, categoryId, "Compra 1", "50.00", "2026-06-10", "1");
        await PostPurchaseAsync(client, cardId, categoryId, "Compra 2", "70.00", "2026-06-12", "1");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(1, await dbContext.CreditCardStatements.CountAsync());
        Assert.Equal(2, await dbContext.CreditCardInstallments.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_ShouldListPurchasesOfTheCard()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var cardId = await CreateCardAsync(client, "Nubank", closingDay: 25, dueDay: 5);
        var categoryId = await CreateExpenseCategoryAsync(client, "Mercado");
        await PostPurchaseAsync(client, cardId, categoryId, "Feira da semana", "80.00", "2026-06-10", "1");

        var html = await client.GetStringAsync($"/cards/{cardId}/purchases");

        Assert.Contains("Feira da semana", html);
        Assert.Contains("Mercado", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Delete_ShouldRemovePurchaseAndInstallments()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var cardId = await CreateCardAsync(client, "Nubank", closingDay: 25, dueDay: 5);
        var categoryId = await CreateExpenseCategoryAsync(client, "Mercado");
        await PostPurchaseAsync(client, cardId, categoryId, "Compra parcelada", "90.00", "2026-06-10", "3");

        Guid purchaseId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            purchaseId = await dbContext.CreditCardPurchases.Select(purchase => purchase.Id).SingleAsync();
        }

        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/cards/{cardId}/purchases/{purchaseId}");
        var response = await client.PostAsync(
            $"/cards/{cardId}/purchases/{purchaseId}/delete",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await verificationContext.CreditCardPurchases.CountAsync());
        Assert.Equal(0, await verificationContext.CreditCardInstallments.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task User_ShouldNotSeePurchasesFromAnotherTenant()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var cardId = await CreateCardAsync(firstClient, "Cartao Privado", closingDay: 25, dueDay: 5);
        var categoryId = await CreateExpenseCategoryAsync(firstClient, "Mercado", "first@osiris.test");
        await PostPurchaseAsync(firstClient, cardId, categoryId, "Compra Sigilosa", "80.00", "2026-06-10", "1");

        Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync($"/cards/{cardId}/purchases")).StatusCode);

        Guid purchaseId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            purchaseId = await dbContext.CreditCardPurchases.Select(purchase => purchase.Id).SingleAsync();
        }

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await secondClient.GetAsync($"/cards/{cardId}/purchases/{purchaseId}")).StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_OnCardFromAnotherTenant_ShouldReturnNotFoundWithoutCreating()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var foreignCardId = await CreateCardAsync(firstClient, "Cartao do A", closingDay: 25, dueDay: 5);
        var ownCategoryId = await CreateExpenseCategoryAsync(secondClient, "Mercado B", "second@osiris.test");

        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(secondClient, "/cards/create");
        var response = await secondClient.PostAsync(
            $"/cards/{foreignCardId}/purchases/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Description"] = "Invasão",
                ["TotalAmount"] = "10.00",
                ["PurchaseDate"] = "2026-06-10",
                ["Installments"] = "1",
                ["CategoryId"] = ownCategoryId.ToString(),
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await dbContext.CreditCardPurchases.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_WithCategoryFromAnotherTenant_ShouldRejectWithoutCreating()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var foreignCategoryId = await CreateExpenseCategoryAsync(firstClient, "Categoria do A", "first@osiris.test");
        var ownCardId = await CreateCardAsync(secondClient, "Cartao do B", closingDay: 25, dueDay: 5);

        var response = await PostPurchaseAsync(
            secondClient,
            ownCardId,
            foreignCategoryId,
            "Compra",
            "10.00",
            "2026-06-10",
            "1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await dbContext.CreditCardPurchases.CountAsync());
    }

    private static async Task<HttpResponseMessage> PostPurchaseAsync(
        HttpClient client,
        Guid cardId,
        Guid categoryId,
        string description,
        string totalAmount,
        string purchaseDate,
        string installments)
    {
        var path = $"/cards/{cardId}/purchases/create";
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, path);

        return await client.PostAsync(
            path,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Description"] = description,
                ["TotalAmount"] = totalAmount,
                ["PurchaseDate"] = purchaseDate,
                ["Installments"] = installments,
                ["CategoryId"] = categoryId.ToString(),
                ["__RequestVerificationToken"] = token
            }));
    }

    private async Task<Guid> CreateCardAsync(HttpClient client, string name, int closingDay, int dueDay)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/cards/create");
        var response = await client.PostAsync(
            "/cards/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Limit"] = "5000.00",
                ["ClosingDay"] = closingDay.ToString(),
                ["DueDay"] = dueDay.ToString(),
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var normalizedName = CreditCard.NormalizeName(name);
        return await dbContext.CreditCards
            .Where(card => card.NormalizedName == normalizedName)
            .Select(card => card.Id)
            .SingleAsync();
    }

    private Task<Guid> CreateExpenseCategoryAsync(
        HttpClient client,
        string name,
        string email = IntegrationTestHelpers.DefaultEmail)
    {
        return IntegrationTestHelpers.GetOrCreateExpenseCategoryAsync(_factory, client, email, name);
    }
}
