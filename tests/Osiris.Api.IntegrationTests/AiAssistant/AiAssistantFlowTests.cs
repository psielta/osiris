using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Api.IntegrationTests.Support;
using Osiris.Infrastructure.Persistence;

namespace Osiris.Api.IntegrationTests.AiAssistant;

public sealed record AiMessageResponse(Guid Id, string Role, string Content, DateTime CreatedAtUtc);

public sealed record AiSourceResponse(string Type, string? Id, string Label);

public sealed record AiTurnResponse(Guid ConversationId, AiMessageResponse Message, List<AiSourceResponse> Sources, bool UsageLimited);

[Collection(ApiIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AiAssistantFlowTests : IAsyncLifetime
{
    private readonly OsirisApiApplicationFactory _factory;

    public AiAssistantFlowTests(OsirisApiApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> AuthenticatedClientAsync(string email = "ai-owner@osiris.test")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: email);
        ApiTestHelpers.Authorize(client, tokens.AccessToken);
        return client;
    }

    [Fact]
    public async Task StartConversation_runs_a_turn_and_persists_it()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "Resuma minha situação de junho." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var turn = (await response.Content.ReadFromJsonAsync<AiTurnResponse>())!;
        Assert.NotEqual(Guid.Empty, turn.ConversationId);
        Assert.Equal("assistant", turn.Message.Role);
        Assert.False(string.IsNullOrWhiteSpace(turn.Message.Content));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageCount = await dbContext.AiMessages.CountAsync(message => message.ConversationId == turn.ConversationId);
        Assert.True(messageCount >= 2, "expected the user message and the assistant reply to be stored");
        Assert.True(await dbContext.AiToolCalls.AnyAsync(call =>
            call.ConversationId == turn.ConversationId && call.ToolName == "get_financial_snapshot"));
    }

    [Fact]
    public async Task Continue_existing_conversation_returns_a_reply()
    {
        var client = await AuthenticatedClientAsync();

        var start = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "Olá" });
        start.EnsureSuccessStatusCode();
        var conversationId = (await start.Content.ReadFromJsonAsync<AiTurnResponse>())!.ConversationId;

        var follow = await client.PostAsJsonAsync(
            $"/api/v1/ai/conversations/{conversationId}/messages",
            new { message = "E quanto às próximas faturas?" });

        Assert.Equal(HttpStatusCode.OK, follow.StatusCode);
        var turn = (await follow.Content.ReadFromJsonAsync<AiTurnResponse>())!;
        Assert.Equal(conversationId, turn.ConversationId);
    }

    [Fact]
    public async Task Conversation_is_isolated_per_tenant()
    {
        var clientA = await AuthenticatedClientAsync("alice@osiris.test");
        var start = await clientA.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "Minha conversa" });
        start.EnsureSuccessStatusCode();
        var conversationId = (await start.Content.ReadFromJsonAsync<AiTurnResponse>())!.ConversationId;

        var clientB = await AuthenticatedClientAsync("bob@osiris.test");
        var hijack = await clientB.PostAsJsonAsync(
            $"/api/v1/ai/conversations/{conversationId}/messages",
            new { message = "Deixa eu ver isso" });

        Assert.Equal(HttpStatusCode.NotFound, hijack.StatusCode);
    }

    [Fact]
    public async Task BlankMessage_returns_400()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "oi" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task When_feature_disabled_endpoint_returns_404()
    {
        var disabledFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:AiAssistant"] = "false"
                });
            });
        });

        var client = disabledFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: "disabled@osiris.test");
        ApiTestHelpers.Authorize(client, tokens.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "oi" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
