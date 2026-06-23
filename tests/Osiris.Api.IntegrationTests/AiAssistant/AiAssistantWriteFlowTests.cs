using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Osiris.Api.IntegrationTests.Support;
using Osiris.Application.Common.AI;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Persistence;

namespace Osiris.Api.IntegrationTests.AiAssistant;

[Collection(ApiIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AiAssistantWriteFlowTests : IAsyncLifetime
{
    private readonly OsirisApiApplicationFactory _factory;

    public AiAssistantWriteFlowTests(OsirisApiApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private WebApplicationFactory<Program> WritesEnabledFactory() =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:AiAssistantWrites"] = "true"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiModelClient>();
                services.AddSingleton<IAiModelClient, WriteProposingFakeAiModelClient>();
            });
        });

    private static async Task<HttpClient> AuthenticatedClientAsync(WebApplicationFactory<Program> factory, string email)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: email);
        ApiTestHelpers.Authorize(client, tokens.AccessToken);
        return client;
    }

    private async Task CreateAccountAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await dbContext.Users.Where(user => user.Email == email).Select(user => user.TenantId).SingleAsync();
        dbContext.FinancialAccounts.Add(new FinancialAccount(tenantId, "Conta IA", FinancialAccountType.CheckingAccount, 1000m));
        await dbContext.SaveChangesAsync();
    }

    private async Task<int> MovementCountAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await dbContext.Users.Where(user => user.Email == email).Select(user => user.TenantId).SingleAsync();
        return await dbContext.FinancialAccountMovements.CountAsync(movement => movement.TenantId == tenantId);
    }

    private static async Task<Guid> ProposeAsync(HttpClient client, string message = "registre uma saída de 50 reais no mercado")
    {
        var response = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message });
        response.EnsureSuccessStatusCode();
        var turn = (await response.Content.ReadFromJsonAsync<AiTurnResponse>())!;
        Assert.Single(turn.Proposals);
        return turn.Proposals[0].Id;
    }

    [Fact]
    public async Task Propose_then_confirm_creates_the_movement_exactly_once()
    {
        const string email = "writer@osiris.test";
        var client = await AuthenticatedClientAsync(WritesEnabledFactory(), email);
        await CreateAccountAsync(email);

        var proposalId = await ProposeAsync(client);

        // Nothing is written until the user confirms.
        Assert.Equal(0, await MovementCountAsync(email));

        var proposal = await client.GetFromJsonAsync<AiProposalResponse>($"/api/v1/ai/actions/{proposalId}");
        Assert.Equal("Pending", proposal!.Status);

        var confirm = await client.PostAsync($"/api/v1/ai/actions/{proposalId}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var actionResult = (await confirm.Content.ReadFromJsonAsync<AiActionResultResponse>())!;
        Assert.Equal("Executed", actionResult.Status);
        Assert.NotNull(actionResult.ResultEntityId);

        Assert.Equal(1, await MovementCountAsync(email));

        // Confirming again is idempotent: no second movement.
        var confirmAgain = await client.PostAsync($"/api/v1/ai/actions/{proposalId}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirmAgain.StatusCode);
        Assert.Equal(1, await MovementCountAsync(email));
    }

    [Fact]
    public async Task Reject_prevents_execution()
    {
        const string email = "rejecter@osiris.test";
        var client = await AuthenticatedClientAsync(WritesEnabledFactory(), email);
        await CreateAccountAsync(email);

        var proposalId = await ProposeAsync(client);

        var reject = await client.PostAsync($"/api/v1/ai/actions/{proposalId}/reject", content: null);
        Assert.Equal(HttpStatusCode.NoContent, reject.StatusCode);

        var proposal = await client.GetFromJsonAsync<AiProposalResponse>($"/api/v1/ai/actions/{proposalId}");
        Assert.Equal("Rejected", proposal!.Status);

        var confirm = await client.PostAsync($"/api/v1/ai/actions/{proposalId}/confirm", content: null);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Equal(0, await MovementCountAsync(email));
    }

    [Fact]
    public async Task With_writes_disabled_no_proposal_is_created()
    {
        // Base factory: writes flag is off, so the propose tool is never offered.
        var client = await AuthenticatedClientAsync(_factory, "nowrite@osiris.test");

        var response = await client.PostAsJsonAsync("/api/v1/ai/conversations", new { message = "registre uma saída de 50 reais" });
        response.EnsureSuccessStatusCode();
        var turn = (await response.Content.ReadFromJsonAsync<AiTurnResponse>())!;

        Assert.Empty(turn.Proposals);
    }

    [Fact]
    public async Task Action_is_isolated_per_tenant()
    {
        var factory = WritesEnabledFactory();
        var clientA = await AuthenticatedClientAsync(factory, "owner-a@osiris.test");
        await CreateAccountAsync("owner-a@osiris.test");
        var proposalId = await ProposeAsync(clientA);

        var clientB = await AuthenticatedClientAsync(factory, "owner-b@osiris.test");

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/ai/actions/{proposalId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PostAsync($"/api/v1/ai/actions/{proposalId}/confirm", content: null)).StatusCode);
    }
}
