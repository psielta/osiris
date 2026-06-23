using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Services;

/// <summary>Builds and persists a pending write proposal. Shared by every write tool so the TTL,
/// idempotency key and persistence are consistent.</summary>
public interface IAiActionProposalFactory
{
    Task<AiActionProposal> CreateAsync(
        AiAgentContext context,
        string actionType,
        string payloadJson,
        string displaySummary,
        string impactSummary,
        string stateHash,
        CancellationToken cancellationToken);
}

public sealed class AiActionProposalFactory : IAiActionProposalFactory
{
    private readonly IAiActionProposalRepository _proposals;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly AiAgentOptions _options;

    public AiActionProposalFactory(
        IAiActionProposalRepository proposals,
        IDateTimeProvider dateTimeProvider,
        IOptions<AiAgentOptions> options)
    {
        _proposals = proposals;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
    }

    public async Task<AiActionProposal> CreateAsync(
        AiAgentContext context,
        string actionType,
        string payloadJson,
        string displaySummary,
        string impactSummary,
        string stateHash,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var proposal = new AiActionProposal(
            context.TenantId,
            context.UserId,
            context.ConversationId,
            actionType,
            payloadJson,
            displaySummary,
            impactSummary,
            AiToolRisk.WriteProposal,
            Guid.NewGuid().ToString("n"),
            stateHash,
            now,
            now.AddMinutes(_options.ProposalTtlMinutes));

        await _proposals.AddAsync(proposal, cancellationToken);
        return proposal;
    }
}
