using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Osiris.Api.IntegrationTests.Support;

public sealed record AuthUserResponse(string FullName, string Email, string TenantName);

public sealed record AuthTokensResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    string TokenType,
    AuthUserResponse User);

public sealed record ProfileResponse(string UserId, string Email, string FullName, Guid TenantId, string TenantName);

internal static class ApiTestHelpers
{
    public static async Task<AuthTokensResponse> RegisterAsync(
        HttpClient client,
        string tenantName = "Acme Finance",
        string fullName = "Jane Owner",
        string email = "jane.owner@osiris.test",
        string password = "password1")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName,
            fullName,
            email,
            password,
            confirmPassword = password
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokensResponse>())!;
    }

    public static void Authorize(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
