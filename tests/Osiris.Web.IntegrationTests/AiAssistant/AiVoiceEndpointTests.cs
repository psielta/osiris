using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.AiAssistant;

[Collection(WebIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AiVoiceEndpointTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public AiVoiceEndpointTests(OsirisWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Voice_endpoint_returns_404_when_voice_flag_is_off()
    {
        // The test host enables AiAssistant but not AiAssistantVoice, so the endpoint behaves as if absent.
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory, email: "voice-off@osiris.test");

        var response = await client.GetAsync("/assistant/voice");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Voice_endpoint_requires_authentication()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/assistant/voice");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); // cookie auth → /Account/Login
    }
}
