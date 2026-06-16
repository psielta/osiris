using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Api.IntegrationTests.Support;
using Osiris.Infrastructure.Persistence;

namespace Osiris.Api.IntegrationTests.Authentication;

[Collection(ApiIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ApiAuthenticationTests : IAsyncLifetime
{
    private readonly OsirisApiApplicationFactory _factory;

    public ApiAuthenticationTests(OsirisApiApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task Register_ShouldReturnTokensWithUser_AndSeedCategories()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Acme Finance",
            fullName = "Jane Owner",
            email = "jane.owner@osiris.test",
            password = "password1",
            confirmPassword = "password1"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrEmpty(tokens!.AccessToken));
        Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));
        Assert.Equal("Bearer", tokens.TokenType);
        Assert.Equal("Acme Finance", tokens.User.TenantName);
        Assert.Equal("Jane Owner", tokens.User.FullName);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenant = await dbContext.Tenants.SingleAsync();
        Assert.True(await dbContext.FinancialCategories.AnyAsync(category => category.TenantId == tenant.Id));
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ShouldReturn400()
    {
        var client = CreateClient();
        await ApiTestHelpers.RegisterAsync(client, email: "dup@osiris.test");

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Other",
            fullName = "Someone Else",
            email = "dup@osiris.test",
            password = "password1",
            confirmPassword = "password1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenPasswordsDoNotMatch_ShouldReturn400()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Acme",
            fullName = "Jane",
            email = "mismatch@osiris.test",
            password = "password1",
            confirmPassword = "password2"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        var client = CreateClient();
        await ApiTestHelpers.RegisterAsync(client, email: "login@osiris.test");

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "login@osiris.test",
            password = "password1"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.False(string.IsNullOrEmpty(tokens!.AccessToken));
        Assert.Equal("login@osiris.test", tokens.User.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        var client = CreateClient();
        await ApiTestHelpers.RegisterAsync(client, email: "wrongpass@osiris.test");

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "wrongpass@osiris.test",
            password = "not-the-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturn401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "nobody@osiris.test",
            password = "password1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ShouldReturn401_NotRedirect()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        // The critical regression guard: the API must challenge with Bearer (401), NOT redirect
        // to the cookie login page (302), proving the JWT default-scheme override took effect.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    [Fact]
    public async Task Me_WithToken_ShouldReturnProfile()
    {
        var client = CreateClient();
        var tokens = await ApiTestHelpers.RegisterAsync(client, tenantName: "Acme Finance", fullName: "Jane Owner", email: "me@osiris.test");
        ApiTestHelpers.Authorize(client, tokens.AccessToken);

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("me@osiris.test", profile!.Email);
        Assert.Equal("Jane Owner", profile.FullName);
        Assert.Equal("Acme Finance", profile.TenantName);
    }

    [Fact]
    public async Task Refresh_ShouldRotate_AndIssueWorkingNewToken()
    {
        var client = CreateClient();
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: "refresh@osiris.test");

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.NotEqual(tokens.RefreshToken, rotated!.RefreshToken);

        // The freshly rotated refresh token works for the next refresh.
        var again = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = rotated.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReusingRotatedToken_ShouldBeRejected()
    {
        var client = CreateClient();
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: "reuse@osiris.test");

        var rotate = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, rotate.StatusCode);

        // Replaying the original (now rotated) token is treated as theft and rejected.
        var reuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Refresh_Concurrent_SameToken_ShouldLetExactlyOneSucceed()
    {
        var client = CreateClient();
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: "race@osiris.test");

        var body = new { refreshToken = tokens.RefreshToken };
        var first = client.PostAsJsonAsync("/api/v1/auth/refresh", body);
        var second = client.PostAsJsonAsync("/api/v1/auth/refresh", body);
        var responses = await Task.WhenAll(first, second);

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized));
    }

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken_AndBeIdempotent()
    {
        var client = CreateClient();
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: "logout@osiris.test");
        ApiTestHelpers.Authorize(client, tokens.AccessToken);

        var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // The revoked refresh token can no longer be used.
        var reuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // Logout is idempotent.
        var secondLogout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, secondLogout.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_ShouldReturn401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = "whatever" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ShouldReflectOwnTenant_ForEachUser()
    {
        var clientA = CreateClient();
        var a = await ApiTestHelpers.RegisterAsync(clientA, tenantName: "Tenant A", fullName: "Alice", email: "alice@osiris.test");
        ApiTestHelpers.Authorize(clientA, a.AccessToken);

        var clientB = CreateClient();
        var b = await ApiTestHelpers.RegisterAsync(clientB, tenantName: "Tenant B", fullName: "Bob", email: "bob@osiris.test");
        ApiTestHelpers.Authorize(clientB, b.AccessToken);

        var profileA = await (await clientA.GetAsync("/api/v1/auth/me")).Content.ReadFromJsonAsync<ProfileResponse>();
        var profileB = await (await clientB.GetAsync("/api/v1/auth/me")).Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal("Tenant A", profileA!.TenantName);
        Assert.Equal("alice@osiris.test", profileA.Email);
        Assert.Equal("Tenant B", profileB!.TenantName);
        Assert.NotEqual(profileA.TenantId, profileB.TenantId);
    }
}
