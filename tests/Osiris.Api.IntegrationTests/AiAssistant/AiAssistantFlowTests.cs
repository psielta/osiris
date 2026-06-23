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

public sealed record AiConversationListResponse(Guid Id, string Title, string Status, DateTime? UpdatedAtUtc, DateTime CreatedAtUtc);

public sealed record AiConversationDetailResponse(Guid Id, string Title, string Status, List<AiMessageResponse> Messages);

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
    public async Task List_and_get_return_the_user_conversation_with_messages()
    {
        var client = await AuthenticatedClientAsync();

        var start = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "Resumo de junho" });
        start.EnsureSuccessStatusCode();
        var conversationId = (await start.Content.ReadFromJsonAsync<AiTurnResponse>())!.ConversationId;

        var list = await client.GetFromJsonAsync<List<AiConversationListResponse>>("/api/v1/ai/conversations");
        Assert.NotNull(list);
        Assert.Contains(list!, conversation => conversation.Id == conversationId && conversation.Status == "Active");

        var detail = await client.GetFromJsonAsync<AiConversationDetailResponse>($"/api/v1/ai/conversations/{conversationId}");
        Assert.NotNull(detail);
        Assert.Equal(conversationId, detail!.Id);
        Assert.Contains(detail.Messages, message => message.Role == "user");
        Assert.Contains(detail.Messages, message => message.Role == "assistant");
    }

    [Fact]
    public async Task Archive_hides_conversation_from_list_and_blocks_new_messages()
    {
        var client = await AuthenticatedClientAsync();

        var start = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "Vou arquivar" });
        start.EnsureSuccessStatusCode();
        var conversationId = (await start.Content.ReadFromJsonAsync<AiTurnResponse>())!.ConversationId;

        var archive = await client.PostAsync($"/api/v1/ai/conversations/{conversationId}/archive", content: null);
        Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);

        var list = await client.GetFromJsonAsync<List<AiConversationListResponse>>("/api/v1/ai/conversations");
        Assert.DoesNotContain(list!, conversation => conversation.Id == conversationId);

        // The archived conversation is still viewable but rejects new messages.
        var detail = await client.GetAsync($"/api/v1/ai/conversations/{conversationId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

        var follow = await client.PostAsJsonAsync(
            $"/api/v1/ai/conversations/{conversationId}/messages",
            new { message = "ainda dá?" });
        Assert.Equal(HttpStatusCode.NotFound, follow.StatusCode);
    }

    [Fact]
    public async Task Get_and_archive_of_another_tenants_conversation_return_404()
    {
        var clientA = await AuthenticatedClientAsync("alice2@osiris.test");
        var start = await clientA.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "Privada de A" });
        start.EnsureSuccessStatusCode();
        var conversationId = (await start.Content.ReadFromJsonAsync<AiTurnResponse>())!.ConversationId;

        var clientB = await AuthenticatedClientAsync("bob2@osiris.test");

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/ai/conversations/{conversationId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PostAsync($"/api/v1/ai/conversations/{conversationId}/archive", content: null)).StatusCode);
        Assert.DoesNotContain(
            (await clientB.GetFromJsonAsync<List<AiConversationListResponse>>("/api/v1/ai/conversations"))!,
            conversation => conversation.Id == conversationId);
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
    public async Task Exceeding_the_daily_token_budget_returns_429()
    {
        var limitedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AiAssistant:DailyTokenLimitPerTenant"] = "5"
                });
            });
        });

        var client = limitedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: "budget@osiris.test");
        ApiTestHelpers.Authorize(client, tokens.AccessToken);

        var first = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "primeira pergunta" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // The first turn already spent more than the tiny budget, so the next one is rejected.
        var second = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "segunda pergunta" });
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
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
