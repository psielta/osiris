using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Api.IntegrationTests.Support;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Persistence;

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

    [Fact]
    public async Task Voice_websocket_persists_voice_transcripts_and_surfaces_write_proposal()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:AiAssistantVoice"] = "true",
                    ["Features:AiAssistantWrites"] = "true",
                    ["AiAssistant:VoiceWritesEnabled"] = "true"
                });
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: "voice-write@osiris.test");

        var wsClient = factory.Server.CreateWebSocketClient();
        wsClient.ConfigureRequest = request =>
        {
            request.Headers.Authorization = $"Bearer {tokens.AccessToken}";
        };

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/api/v1/ai/voice"), timeout.Token);

        var payloads = new List<JsonDocument>();
        while (!payloads.Any(payload => payload.RootElement.GetProperty("type").GetString() == "proposal")
            || !payloads.Any(payload =>
                payload.RootElement.GetProperty("type").GetString() == "status"
                && payload.RootElement.GetProperty("value").GetString() == "idle"))
        {
            payloads.Add(JsonDocument.Parse(await ReceiveTextAsync(socket, timeout.Token)));
        }

        var session = payloads.Single(payload => payload.RootElement.GetProperty("type").GetString() == "session");
        var conversationId = session.RootElement.GetProperty("conversationId").GetGuid();

        var proposalPayload = payloads.Single(payload => payload.RootElement.GetProperty("type").GetString() == "proposal");
        Assert.Equal("Criar conta a pagar \"Internet\" de R$ 10,00 (vence 30/06/2026)",
            proposalPayload.RootElement.GetProperty("proposal").GetProperty("displaySummary").GetString());

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var conversation = await dbContext.AiConversations.SingleAsync(timeout.Token);
        Assert.Equal(conversation.Id, conversationId);

        var messages = await dbContext.AiMessages
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.CreatedAtUtc)
            .ToListAsync(timeout.Token);

        Assert.Contains(messages, message => message.Role == AiMessageRole.User
            && message.Channel == "voice"
            && message.Content.Contains("conta de internet", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(messages, message => message.Role == AiMessageRole.Assistant
            && message.Channel == "voice"
            && message.Content.Contains("Confirme na tela", StringComparison.OrdinalIgnoreCase));

        var proposal = await dbContext.AiActionProposals.SingleAsync(timeout.Token);
        Assert.Equal(conversationId, proposal.ConversationId);

        var fakeLive = factory.Services.GetRequiredService<FakeAiLiveSessionClient>();
        Assert.Contains(fakeLive.Requests.Single().Tools, tool => tool.Name == "propose_bill_creation");
        Assert.Contains(fakeLive.ToolResults.Single(), result => result.Id == "call-1");
    }

    private static async Task<string> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var message = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken);
            Assert.NotEqual(WebSocketMessageType.Close, result.MessageType);
            message.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        return Encoding.UTF8.GetString(message.ToArray());
    }
}
