using System.Text.Json;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.AiAssistant.Tools.Proposals;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class ProposeBillCreationToolTests
{
    private static AiAgentContext Context(Guid tenantId) =>
        new(tenantId, "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 22), WritesEnabled: true);

    [Fact]
    public async Task Creates_a_pending_bill_proposal_without_executing()
    {
        var tenantId = Guid.NewGuid();
        var repo = new FakeAiActionProposalRepository();
        var factory = new AiActionProposalFactory(repo, new FakeDateTimeProvider(), Options.Create(new AiAgentOptions()));
        var tool = new ProposeBillCreationTool(factory);

        var arguments = JsonDocument.Parse(
            $"{{\"description\":\"Aluguel\",\"amount\":1200,\"dueDate\":\"2026-07-10\",\"categoryId\":\"{Guid.NewGuid()}\"}}").RootElement;

        var result = await tool.ExecuteAsync(arguments, Context(tenantId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AiToolRisk.WriteProposal, tool.Risk);
        Assert.NotNull(result.Proposals);
        Assert.Single(result.Proposals!);

        var proposal = Assert.Single(repo.Proposals);
        Assert.Equal(AiActionTypes.BillCreation, proposal.ActionType);
        Assert.Equal(AiActionProposalStatus.Pending, proposal.Status);
        Assert.Equal(tenantId, proposal.TenantId);
    }
}
