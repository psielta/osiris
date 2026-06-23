using System.Text.Json;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.AiAssistant.Commands.RejectAction;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class RejectActionCommandHandlerTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTime Now = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly Guid _tenantId = Guid.NewGuid();

    private AiActionProposal BuildProposal()
    {
        var payload = new ManualMovementPayload(Guid.NewGuid(), "Expense", 50m, new DateOnly(2026, 6, 22), "Mercado", null, null);
        return new AiActionProposal(
            _tenantId,
            "user-1",
            Guid.NewGuid(),
            AiActionTypes.ManualMovement,
            JsonSerializer.Serialize(payload, SerializerOptions),
            "display",
            "impact",
            AiToolRisk.WriteProposal,
            "idem-1",
            "hash",
            Now,
            Now.AddMinutes(15));
    }

    private RejectActionCommandHandler CreateHandler(FakeAiActionProposalRepository repo) =>
        new(repo, new FakeCurrentUser(_tenantId));

    [Fact]
    public async Task Reject_pending_proposal_marks_it_rejected_and_is_idempotent()
    {
        var proposal = BuildProposal();
        var repo = new FakeAiActionProposalRepository(proposal);
        var handler = CreateHandler(repo);

        var result = await handler.Handle(new RejectActionCommand(proposal.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AiActionProposalStatus.Rejected, proposal.Status);

        var again = await handler.Handle(new RejectActionCommand(proposal.Id), CancellationToken.None);
        Assert.True(again.IsSuccess);
    }

    [Fact]
    public async Task Reject_of_unknown_proposal_returns_not_found()
    {
        var handler = CreateHandler(new FakeAiActionProposalRepository());

        var result = await handler.Handle(new RejectActionCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
    }
}
