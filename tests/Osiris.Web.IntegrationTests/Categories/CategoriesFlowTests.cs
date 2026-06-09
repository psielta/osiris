using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.Categories;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class CategoriesFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public CategoriesFlowTests(OsirisWebApplicationFactory factory)
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

        var response = await client.GetAsync("/categories");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "http://localhost/Account/Login?ReturnUrl=%2Fcategories",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_WhenAuthenticated_ShouldReturnOk()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await client.GetAsync("/categories");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Osiris v0.3.0", html);
        Assert.DoesNotContain("SaaS skeleton", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_ShouldMarkCategoriesNavigationAsActive()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/categories");

        Assert.Matches(
            new Regex("<a(?=[^>]*href=\"/categories\")(?=[^>]*aria-current=\"page\")[^>]*>", RegexOptions.IgnoreCase),
            html);
        Assert.DoesNotMatch(
            new Regex("<a(?=[^>]*href=\"/dashboard\")(?=[^>]*aria-current=\"page\")[^>]*>", RegexOptions.IgnoreCase),
            html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_WhenEmpty_ShouldShowIconEmptyState()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/categories");

        Assert.Contains("ph ph-tag-simple", html);
        Assert.Contains("Nenhuma categoria ativa ainda", html);
        Assert.Contains("receita ou despesa", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_ShouldUseColorPicker()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/categories/create");

        Assert.Contains("type=\"color\"", html);
        Assert.Contains("Selecione o tipo", html);
        Assert.Contains(">Receita</option>", html);
        Assert.Contains(">Despesa</option>", html);
        Assert.DoesNotContain("placeholder=\"#A1B2C3\"", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateEditArchiveDelete_ShouldPersistExpectedCategoryFlow()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var createResponse = await PostCategoryAsync(client, "/categories/create", "Food", "2", "#A1B2C3");

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var category = await SingleCategoryAsync();
        Assert.Equal("Food", category.Name);
        Assert.Equal("FOOD", category.NormalizedName);
        Assert.Equal(CategoryType.Expense, category.Type);
        Assert.True(category.IsActive);

        var editToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/categories/{category.Id}/edit");
        var editResponse = await client.PostAsync(
            $"/categories/{category.Id}/edit",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = "Groceries",
                ["Type"] = "2",
                ["Color"] = "#010203",
                ["__RequestVerificationToken"] = editToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
        category = await SingleCategoryAsync();
        Assert.Equal("Groceries", category.Name);
        Assert.Equal("#010203", category.Color);

        var archiveToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/categories");
        var archiveResponse = await client.PostAsync(
            $"/categories/{category.Id}/archive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = archiveToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, archiveResponse.StatusCode);
        category = await SingleCategoryAsync();
        Assert.False(category.IsActive);

        var deleteToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/categories/{category.Id}/delete");
        var deleteResponse = await client.PostAsync(
            $"/categories/{category.Id}/delete",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = deleteToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);
        Assert.Equal(0, await CategoryCountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_WhenTypeMissingOrInvalid_ShouldRejectWithoutCreatingRow()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var missingType = await PostCategoryAsync(client, "/categories/create", "Food", type: null, color: null);

        Assert.Equal(HttpStatusCode.OK, missingType.StatusCode);
        Assert.Equal(0, await CategoryCountAsync());

        var invalidType = await PostCategoryAsync(client, "/categories/create", "Food", "999", null);

        Assert.Equal(HttpStatusCode.OK, invalidType.StatusCode);
        Assert.Equal(0, await CategoryCountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_WhenDuplicateNameAndTypeInSameTenant_ShouldReject()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        Assert.Equal(HttpStatusCode.Redirect, (await PostCategoryAsync(client, "/categories/create", "Food", "2", null)).StatusCode);

        var duplicate = await PostCategoryAsync(client, "/categories/create", "Food", "2", null);

        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Equal(1, await CategoryCountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_ShouldBlockCaseVariantDuplicateInSameTenant()
    {
        await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await dbContext.Tenants.Select(tenant => tenant.Id).SingleAsync();
        dbContext.FinancialCategories.Add(new FinancialCategory(tenantId, "Food", CategoryType.Expense));
        await dbContext.SaveChangesAsync();

        dbContext.FinancialCategories.Add(new FinancialCategory(tenantId, "food", CategoryType.Expense));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ArchivedName_ShouldStayReservedUntilPhysicalDelete()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        Assert.Equal(HttpStatusCode.Redirect, (await PostCategoryAsync(client, "/categories/create", "Food", "2", null)).StatusCode);
        var category = await SingleCategoryAsync();

        var archiveToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/categories");
        var archiveResponse = await client.PostAsync(
            $"/categories/{category.Id}/archive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = archiveToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, archiveResponse.StatusCode);
        var duplicateWhileArchived = await PostCategoryAsync(client, "/categories/create", "Food", "2", null);

        Assert.Equal(HttpStatusCode.OK, duplicateWhileArchived.StatusCode);
        Assert.Equal(1, await CategoryCountAsync());

        var deleteToken = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/categories/{category.Id}/delete");
        var deleteResponse = await client.PostAsync(
            $"/categories/{category.Id}/delete",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = deleteToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        var recreate = await PostCategoryAsync(client, "/categories/create", "Food", "2", null);

        Assert.Equal(HttpStatusCode.Redirect, recreate.StatusCode);
        Assert.Equal(1, await CategoryCountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SameNameAndType_ShouldBeAllowedAcrossTenants()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");

        Assert.Equal(HttpStatusCode.Redirect, (await PostCategoryAsync(firstClient, "/categories/create", "Food", "2", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostCategoryAsync(secondClient, "/categories/create", "Food", "2", null)).StatusCode);
        Assert.Equal(2, await CategoryCountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task User_ShouldNotSeeOrMutateCategoriesFromAnotherTenant()
    {
        var firstClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "First Tenant",
            email: "first@osiris.test");
        var secondClient = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(
            _factory,
            tenantName: "Second Tenant",
            email: "second@osiris.test");
        Assert.Equal(HttpStatusCode.Redirect, (await PostCategoryAsync(firstClient, "/categories/create", "Private Food", "2", null)).StatusCode);
        var category = await SingleCategoryAsync();

        var listing = await secondClient.GetStringAsync("/categories");

        Assert.DoesNotContain("Private Food", listing);

        var edit = await secondClient.GetAsync($"/categories/{category.Id}/edit");

        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);

        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(secondClient, "/categories/create");
        var archive = await secondClient.PostAsync(
            $"/categories/{category.Id}/archive",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        var delete = await secondClient.PostAsync(
            $"/categories/{category.Id}/delete",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.NotFound, archive.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        category = await SingleCategoryAsync();
        Assert.True(category.IsActive);
    }

    private static async Task<HttpResponseMessage> PostCategoryAsync(
        HttpClient client,
        string path,
        string name,
        string? type,
        string? color)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, path);
        var values = new Dictionary<string, string>
        {
            ["Name"] = name,
            ["__RequestVerificationToken"] = token
        };

        if (type is not null)
        {
            values["Type"] = type;
        }

        if (color is not null)
        {
            values["Color"] = color;
        }

        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }

    private async Task<FinancialCategory> SingleCategoryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.FinancialCategories.SingleAsync();
    }

    private async Task<int> CategoryCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.FinancialCategories.CountAsync();
    }
}
