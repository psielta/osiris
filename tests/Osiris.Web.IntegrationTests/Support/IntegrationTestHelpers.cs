using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Persistence;

namespace Osiris.Web.IntegrationTests.Support;

internal static class IntegrationTestHelpers
{
    public const string DefaultEmail = "jane.owner@osiris.test";

    /// <summary>
    /// Resolves an expense category for the user's tenant, reusing a seeded/default category of
    /// the same name when one exists and creating it through the UI otherwise.
    /// </summary>
    public static async Task<Guid> GetOrCreateExpenseCategoryAsync(
        OsirisWebApplicationFactory factory,
        HttpClient client,
        string userEmail,
        string name)
    {
        var tenantId = await GetTenantIdAsync(factory, userEmail);
        var normalizedName = FinancialCategory.NormalizeName(name);

        var existingId = await FindCategoryIdAsync(factory, tenantId, normalizedName);
        if (existingId is not null)
        {
            return existingId.Value;
        }

        var token = await GetAntiForgeryTokenAsync(client, "/categories/create");
        var response = await client.PostAsync(
            "/categories/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Type"] = ((int)CategoryType.Expense).ToString(),
                ["__RequestVerificationToken"] = token
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var createdId = await FindCategoryIdAsync(factory, tenantId, normalizedName);
        Assert.NotNull(createdId);
        return createdId.Value;
    }

    public static async Task<Guid> GetTenantIdAsync(OsirisWebApplicationFactory factory, string userEmail)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Users
            .Where(user => user.Email == userEmail)
            .Select(user => user.TenantId)
            .SingleAsync();
    }

    private static async Task<Guid?> FindCategoryIdAsync(
        OsirisWebApplicationFactory factory,
        Guid tenantId,
        string normalizedName)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ids = await dbContext.FinancialCategories
            .Where(category => category.TenantId == tenantId && category.NormalizedName == normalizedName)
            .Select(category => category.Id)
            .ToArrayAsync();

        return ids.Length == 0 ? null : ids.Single();
    }

    public static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var inputMatch = Regex.Match(
            html,
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        Assert.True(inputMatch.Success, "The antiforgery token input was not found.");

        var valueMatch = Regex.Match(
            inputMatch.Value,
            "value=\"(?<value>[^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        Assert.True(valueMatch.Success, "The antiforgery token value was not found.");

        return WebUtility.HtmlDecode(valueMatch.Groups["value"].Value);
    }

    public static async Task<HttpClient> RegisterAndAuthenticateAsync(
        OsirisWebApplicationFactory factory,
        string tenantName = "Acme Finance",
        string fullName = "Jane Owner",
        string email = "jane.owner@osiris.test")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var token = await GetAntiForgeryTokenAsync(client, "/account/register");
        var response = await client.PostAsync(
            "/account/register",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["TenantName"] = tenantName,
                ["FullName"] = fullName,
                ["Email"] = email,
                ["Password"] = "password1",
                ["ConfirmPassword"] = "password1",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        return client;
    }
}
