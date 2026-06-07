using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Osiris.Web.IntegrationTests.Support;

internal static class IntegrationTestHelpers
{
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
