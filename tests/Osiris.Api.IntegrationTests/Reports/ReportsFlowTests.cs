using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Osiris.Api.IntegrationTests.Support;

namespace Osiris.Api.IntegrationTests.Reports;

[Collection(ApiIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ReportsFlowTests : IAsyncLifetime
{
    private readonly OsirisApiApplicationFactory _factory;

    public ReportsFlowTests(OsirisApiApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CashFlowSyntheticPdf_WhenAuthenticated_ShouldReturnPdf()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/reports/cash-flow/synthetic/pdf?month=6&year=2026");

        await AssertPdfAttachmentAsync(response);
    }

    [Fact]
    public async Task CashFlowAnalyticPdf_WhenAuthenticated_ShouldReturnPdf()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/reports/cash-flow/analytic/pdf?month=6&year=2026");

        await AssertPdfAttachmentAsync(response);
    }

    [Fact]
    public async Task CashFlowPdf_WhenAnonymous_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v1/reports/cash-flow/synthetic/pdf?month=6&year=2026");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email = "owner@osiris.test")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: email);
        ApiTestHelpers.Authorize(client, tokens.AccessToken);
        return client;
    }

    private static async Task AssertPdfAttachmentAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
