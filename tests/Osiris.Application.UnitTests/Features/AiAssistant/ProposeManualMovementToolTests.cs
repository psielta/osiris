using System.Text.Json;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Tools.Proposals;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Application.Features.FinancialAccounts.DTOs;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class ProposeManualMovementToolTests
{
    private static AiAgentContext Context(Guid tenantId) =>
        new(tenantId, "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 22), WritesEnabled: true);

    private static FinancialAccountStatementDto Account(Guid id) =>
        new(id, "Conta Corrente", FinancialAccountType.CheckingAccount, 0m, 1000m, true, Array.Empty<MovementListItemDto>());

    private static ProposeManualMovementTool CreateTool(object accountResponse, FakeAiActionProposalRepository repo) =>
        new(new FakeSender(accountResponse), repo, new FakeDateTimeProvider(), Options.Create(new AiAgentOptions()));

    [Fact]
    public async Task Creates_a_pending_proposal_without_executing_anything()
    {
        var tenantId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var repo = new FakeAiActionProposalRepository();
        var tool = CreateTool(Account(accountId), repo);

        var arguments = JsonDocument.Parse(
            $"{{\"accountId\":\"{accountId}\",\"type\":\"expense\",\"amount\":50,\"description\":\"Mercado\"}}").RootElement;

        var result = await tool.ExecuteAsync(arguments, Context(tenantId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AiToolRisk.WriteProposal, tool.Risk);
        Assert.NotNull(result.Proposals);
        Assert.Single(result.Proposals!);

        var proposal = Assert.Single(repo.Proposals);
        Assert.Equal(AiActionTypes.ManualMovement, proposal.ActionType);
        Assert.Equal(AiActionProposalStatus.Pending, proposal.Status);
        Assert.Equal(tenantId, proposal.TenantId);
        Assert.Contains("50", proposal.PayloadJson);
    }

    [Fact]
    public async Task Fails_without_creating_a_proposal_when_account_id_is_invalid()
    {
        var repo = new FakeAiActionProposalRepository();
        var tool = CreateTool(new object(), repo);

        var arguments = JsonDocument.Parse("{\"type\":\"expense\",\"amount\":50,\"description\":\"Mercado\"}").RootElement;

        var result = await tool.ExecuteAsync(arguments, Context(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(repo.Proposals);
    }
}
