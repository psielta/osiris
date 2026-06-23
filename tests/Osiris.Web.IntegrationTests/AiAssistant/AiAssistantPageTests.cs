using System.Net;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.AiAssistant;

[Collection(WebIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AiAssistantPageTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public AiAssistantPageTests(OsirisWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Assistant_page_loads_for_authenticated_user()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory, email: "assistant-web@osiris.test");

        var page = await client.GetStringAsync("/assistant");

        Assert.Contains("Assistente financeiro", page);
        Assert.Contains("Nova conversa", page);
    }

    [Fact]
    public async Task Sending_a_message_creates_a_conversation_and_shows_the_reply()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory, email: "assistant-send@osiris.test");
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/assistant");

        var send = await client.PostAsync("/assistant/send", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["conversationId"] = string.Empty,
            ["message"] = "Resuma minha situação de junho.",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, send.StatusCode);
        var location = send.Headers.Location!.ToString();
        Assert.Contains("/assistant", location, StringComparison.OrdinalIgnoreCase);

        // Razor's default HtmlEncoder escapes accented characters to numeric entities, so assert on an
        // accent-free slice of the assistant reply ("Aqui está o seu panorama financeiro do mês.").
        var page = await client.GetStringAsync(location);
        Assert.Contains("panorama financeiro", page);
    }
}
