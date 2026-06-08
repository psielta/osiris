using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.FinancialAccounts;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class FinancialAccountsFlowTests : IAsyncLifetime
{
    private const string IncomeType = "1";
    private const string ExpenseType = "2";

    private readonly OsirisWebApplicationFactory _factory;

    public FinancialAccountsFlowTests(OsirisWebApplicationFactory factory)
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

        var response = await client.GetAsync("/accounts");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "http://localhost/Account/Login?ReturnUrl=%2Faccounts",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_WhenAuthenticated_ShouldReturnOkWithNavigationAndVersion()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await client.GetAsync("/accounts");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Contas", html);
        Assert.Contains("Osiris v0.2.0", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_WhenEmpty_ShouldShowIconEmptyState()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/accounts");

        Assert.Contains("ph ph-bank", html);
        Assert.Contains("Nenhuma conta ativa ainda", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_ShouldPersistAccountWithCurrentBalanceEqualToInitialBalance()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await PostAccountAsync(client, "Banco", IncomeAccountType(), "200.00");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var account = await SingleAccountAsync();
        Assert.Equal("Banco", account.Name);
        Assert.Equal("BANCO", account.NormalizedName);
        Assert.Equal(200.00m, account.InitialBalance);
        Assert.Equal(200.00m, account.CurrentBalance);
        Assert.True(account.IsActive);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateMovement_ShouldUpdateBalanceAndListOnStatement()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAccountAsync(client, "Banco", IncomeAccountType(), "200.00")).StatusCode);
        var account = await SingleAccountAsync();

        var income = await PostMovementAsync(client, account.Id, IncomeType, "50.00", "Pagamento");
        Assert.Equal(HttpStatusCode.Redirect, income.StatusCode);
        Assert.Equal(250.00m, (await FindAccountAsync(account.Id))!.CurrentBalance);

        var expense = await PostMovementAsync(client, account.Id, ExpenseType, "30.00", "Mercado");
        Assert.Equal(HttpStatusCode.Redirect, expense.StatusCode);
        Assert.Equal(220.00m, (await FindAccountAsync(account.Id))!.CurrentBalance);

        Assert.Equal(2, await MovementCountAsync());

        var statement = await client.GetStringAsync($"/accounts/{account.Id}");
        Assert.Contains("Pagamento", statement);
        Assert.Contains("Mercado", statement);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateMovement_WhenInvalid_ShouldRejectWithoutChangingBalance()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAccountAsync(client, "Banco", IncomeAccountType(), "200.00")).StatusCode);
        var account = await SingleAccountAsync();

        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/accounts/{account.Id}");
        var response = await client.PostAsync(
            $"/accounts/{account.Id}/movements",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Movement.Type"] = ExpenseType,
                ["Movement.OccurredOn"] = "2026-06-08",
                ["Movement.Description"] = "Sem valor",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await MovementCountAsync());
        Assert.Equal(200.00m, (await FindAccountAsync(account.Id))!.CurrentBalance);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_ShouldBlockCaseVariantDuplicateNameInSameTenant()
    {
        await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await dbContext.Tenants.Select(tenant => tenant.Id).SingleAsync();
        dbContext.FinancialAccounts.Add(new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m));
        await dbContext.SaveChangesAsync();

        dbContext.FinancialAccounts.Add(new FinancialAccount(tenantId, "banco", FinancialAccountType.Cash, 0m));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SameName_ShouldBeAllowedAcrossTenants()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        Assert.Equal(HttpStatusCode.Redirect, (await PostAccountAsync(firstClient, "Banco", IncomeAccountType(), "0.00")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAccountAsync(secondClient, "Banco", IncomeAccountType(), "0.00")).StatusCode);

        Assert.Equal(2, await AccountCountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task User_ShouldNotSeeOrMutateAccountsFromAnotherTenant()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        Assert.Equal(HttpStatusCode.Redirect, (await PostAccountAsync(firstClient, "Conta Privada", IncomeAccountType(), "100.00")).StatusCode);
        var account = await SingleAccountAsync();

        var listing = await secondClient.GetStringAsync("/accounts");
        Assert.DoesNotContain("Conta Privada", listing);

        Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync($"/accounts/{account.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync($"/accounts/{account.Id}/edit")).StatusCode);

        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(secondClient, "/accounts/create");

        var archive = await secondClient.PostAsync(
            $"/accounts/{account.Id}/archive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.NotFound, archive.StatusCode);

        var movement = await secondClient.PostAsync(
            $"/accounts/{account.Id}/movements",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Movement.Type"] = IncomeType,
                ["Movement.Amount"] = "10.00",
                ["Movement.OccurredOn"] = "2026-06-08",
                ["Movement.Description"] = "Invasão",
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.NotFound, movement.StatusCode);

        var unchanged = await FindAccountAsync(account.Id);
        Assert.NotNull(unchanged);
        Assert.True(unchanged!.IsActive);
        Assert.Equal(100.00m, unchanged.CurrentBalance);
        Assert.Equal(0, await MovementCountAsync());
    }

    private static string IncomeAccountType() => ((int)FinancialAccountType.CheckingAccount).ToString();

    private static async Task<HttpResponseMessage> PostAccountAsync(
        HttpClient client,
        string name,
        string type,
        string initialBalance)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/accounts/create");
        return await client.PostAsync(
            "/accounts/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Type"] = type,
                ["InitialBalance"] = initialBalance,
                ["__RequestVerificationToken"] = token
            }));
    }

    private static async Task<HttpResponseMessage> PostMovementAsync(
        HttpClient client,
        Guid accountId,
        string type,
        string amount,
        string description)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/accounts/{accountId}");
        return await client.PostAsync(
            $"/accounts/{accountId}/movements",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Movement.Type"] = type,
                ["Movement.Amount"] = amount,
                ["Movement.OccurredOn"] = "2026-06-08",
                ["Movement.Description"] = description,
                ["__RequestVerificationToken"] = token
            }));
    }

    private async Task<FinancialAccount> SingleAccountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.FinancialAccounts.SingleAsync();
    }

    private async Task<FinancialAccount?> FindAccountAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.FinancialAccounts.SingleOrDefaultAsync(account => account.Id == id);
    }

    private async Task<int> AccountCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.FinancialAccounts.CountAsync();
    }

    private async Task<int> MovementCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.FinancialAccountMovements.CountAsync();
    }
}
