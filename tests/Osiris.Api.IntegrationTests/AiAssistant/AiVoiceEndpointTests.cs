using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Osiris.Api.IntegrationTests.Support;

namespace Osiris.Api.IntegrationTests.AiAssistant;

[Collection(ApiIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AiVoiceEndpointTests : IAsyncLifetime
{
    private readonly OsirisApiApplicationFactory _factory;

    public AiVoiceEndpointTests(OsirisApiApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Voice_endpoint_returns_404_when_voice_flag_is_off()
    {
        // The test host enables AiAssistant but not AiAssistantVoice.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: "voice-api@osiris.test");
        ApiTestHelpers.Authorize(client, tokens.AccessToken);

        var response = await client.GetAsync("/api/v1/ai/voice");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Voice_endpoint_requires_authentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/ai/voice");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
