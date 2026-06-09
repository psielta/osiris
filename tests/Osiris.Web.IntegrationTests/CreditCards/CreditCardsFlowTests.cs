using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.CreditCards;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class CreditCardsFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public CreditCardsFlowTests(OsirisWebApplicationFactory factory)
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

        var response = await client.GetAsync("/cards");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "http://localhost/Account/Login?ReturnUrl=%2Fcards",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_WhenAuthenticated_ShouldReturnOkWithVersionAndEmptyState()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await client.GetAsync("/cards");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Osiris v0.3.1", html);
        Assert.Contains("ph ph-credit-card", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_ShouldMarkCreditCardsNavigationAsActive()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/cards");

        Assert.Matches(
            new Regex("<a(?=[^>]*href=\"/cards\")(?=[^>]*aria-current=\"page\")[^>]*>", RegexOptions.IgnoreCase),
            html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_ShouldPersistCreditCard()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await PostCardAsync(client, "/cards/create", "Nubank", "1500.00", "3", "10");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var card = await SingleCardAsync();
        Assert.Equal("Nubank", card.Name);
        Assert.Equal("NUBANK", card.NormalizedName);
        Assert.Equal(1500.00m, card.Limit);
        Assert.Equal(3, card.ClosingDay);
        Assert.Equal(10, card.DueDay);
        Assert.Null(card.PaymentAccountId);
        Assert.True(card.IsActive);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_WithPaymentAccountInSameTenant_ShouldPersistWithAccount()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var accountId = await CreateAccountAsync(client, "Banco");

        var response = await PostCardAsync(client, "/cards/create", "Nubank", "0.00", "3", "10", accountId);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var card = await SingleCardAsync();
        Assert.Equal(accountId, card.PaymentAccountId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateEditArchive_ShouldPersistExpectedFlow()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        Assert.Equal(HttpStatusCode.Redirect, (await PostCardAsync(client, "/cards/create", "Nubank", "1000.00", "3", "10")).StatusCode);
        var card = await SingleCardAsync();

        var editResponse = await PostCardAsync(client, $"/cards/{card.Id}/edit", "Inter", "2000.00", "5", "15");
        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
        card = await SingleCardAsync();
        Assert.Equal("Inter", card.Name);
        Assert.Equal(2000.00m, card.Limit);
        Assert.Equal(5, card.ClosingDay);
        Assert.Equal(15, card.DueDay);

        var archiveToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/cards");
        var archiveResponse = await client.PostAsync(
            $"/cards/{card.Id}/archive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = archiveToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, archiveResponse.StatusCode);
        card = await SingleCardAsync();
        Assert.False(card.IsActive);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_WhenInvalid_ShouldRejectWithoutCreatingRow()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await PostCardAsync(client, "/cards/create", "Nubank", "1000.00", "0", "40");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await CardCountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_ShouldBlockCaseVariantDuplicateNameInSameTenant()
    {
        await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await dbContext.Tenants.Select(tenant => tenant.Id).SingleAsync();
        dbContext.CreditCards.Add(new CreditCard(tenantId, "Nubank", 0m, 1, 1, null));
        await dbContext.SaveChangesAsync();

        dbContext.CreditCards.Add(new CreditCard(tenantId, "nubank", 0m, 1, 1, null));

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

        Assert.Equal(HttpStatusCode.Redirect, (await PostCardAsync(firstClient, "/cards/create", "Nubank", "0.00", "1", "1")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostCardAsync(secondClient, "/cards/create", "Nubank", "0.00", "1", "1")).StatusCode);

        Assert.Equal(2, await CardCountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task User_ShouldNotSeeOrMutateCreditCardsFromAnotherTenant()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        Assert.Equal(HttpStatusCode.Redirect, (await PostCardAsync(firstClient, "/cards/create", "Cartao Privado", "100.00", "3", "10")).StatusCode);
        var card = await SingleCardAsync();

        var listing = await secondClient.GetStringAsync("/cards");
        Assert.DoesNotContain("Cartao Privado", listing);

        Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync($"/cards/{card.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync($"/cards/{card.Id}/edit")).StatusCode);

        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(secondClient, "/cards/create");

        var archive = await secondClient.PostAsync(
            $"/cards/{card.Id}/archive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.NotFound, archive.StatusCode);

        var edit = await secondClient.PostAsync(
            $"/cards/{card.Id}/edit",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = "Invadido",
                ["Limit"] = "0.00",
                ["ClosingDay"] = "1",
                ["DueDay"] = "1",
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);

        var unchanged = await FindCardAsync(card.Id);
        Assert.NotNull(unchanged);
        Assert.True(unchanged!.IsActive);
        Assert.Equal("Cartao Privado", unchanged.Name);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_WithPaymentAccountFromAnotherTenant_ShouldReject()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        var firstTenantAccountId = await CreateAccountAsync(firstClient, "Banco do A");

        var response = await PostCardAsync(secondClient, "/cards/create", "Nubank", "0.00", "1", "1", firstTenantAccountId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await CardCountAsync());
    }

    private static async Task<HttpResponseMessage> PostCardAsync(
        HttpClient client,
        string path,
        string name,
        string limit,
        string closingDay,
        string dueDay,
        Guid? paymentAccountId = null)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, path);
        var values = new Dictionary<string, string>
        {
            ["Name"] = name,
            ["Limit"] = limit,
            ["ClosingDay"] = closingDay,
            ["DueDay"] = dueDay,
            ["__RequestVerificationToken"] = token
        };

        if (paymentAccountId is not null)
        {
            values["PaymentAccountId"] = paymentAccountId.Value.ToString();
        }

        return await client.PostAsync(path, new FormUrlEncodedContent(values));
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
                ["InitialBalance"] = "0.00",
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

    private async Task<CreditCard> SingleCardAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.CreditCards.SingleAsync();
    }

    private async Task<CreditCard?> FindCardAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.CreditCards.SingleOrDefaultAsync(card => card.Id == id);
    }

    private async Task<int> CardCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.CreditCards.CountAsync();
    }
}
