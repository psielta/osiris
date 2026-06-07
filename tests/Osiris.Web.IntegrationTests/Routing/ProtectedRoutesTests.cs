using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.Routing;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class ProtectedRoutesTests
{
    private readonly OsirisWebApplicationFactory _factory;

    public ProtectedRoutesTests(OsirisWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Dashboard_WhenAnonymous_ShouldRedirectToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "http://localhost/Account/Login?ReturnUrl=%2Fdashboard",
            response.Headers.Location?.OriginalString);
    }
}
