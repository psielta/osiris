using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.Authentication;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class RegistrationFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public RegistrationFlowTests(OsirisWebApplicationFactory factory)
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
    public async Task Register_ShouldCreateTenantUserAndAuthenticateDashboardAccess()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var token = await GetAntiForgeryTokenAsync(client, "/account/register");

        var response = await client.PostAsync(
            "/account/register",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["TenantName"] = "Acme Finance",
                ["FullName"] = "Jane Owner",
                ["Email"] = "jane.owner@osiris.test",
                ["Password"] = "password1",
                ["ConfirmPassword"] = "password1",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/dashboard", response.Headers.Location?.OriginalString);

        var dashboard = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenant = await dbContext.Tenants.SingleAsync();
        var user = await dbContext.Users.SingleAsync();

        Assert.Equal("Acme Finance", tenant.Name);
        Assert.Equal("Jane Owner", user.FullName);
        Assert.Equal(tenant.Id, user.TenantId);
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path)
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
}
