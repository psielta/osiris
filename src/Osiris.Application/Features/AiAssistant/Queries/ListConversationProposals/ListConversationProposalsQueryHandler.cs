using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Application.Features.AiAssistant.Queries.ListConversationProposals;

public sealed class ListConversationProposalsQueryHandler
    : IRequestHandler<ListConversationProposalsQuery, IReadOnlyCollection<AiActionProposalDto>>
{
    private readonly IAiActionProposalRepository _proposals;
    private readonly ICurrentUser _currentUser;

    public ListConversationProposalsQueryHandler(IAiActionProposalRepository proposals, ICurrentUser currentUser)
    {
        _proposals = proposals;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<AiActionProposalDto>> Handle(
        ListConversationProposalsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Array.Empty<AiActionProposalDto>();
        }

        var proposals = await _proposals.ListPendingForConversationAsync(
            _currentUser.TenantId,
            userId,
            request.ConversationId,
            cancellationToken);

        return proposals
            .Select(proposal => new AiActionProposalDto(
                proposal.Id,
                proposal.ActionType,
                proposal.DisplaySummary,
                proposal.ImpactSummary,
                proposal.RiskLevel.ToString(),
                proposal.Status.ToString(),
                proposal.CreatedAtUtc,
                proposal.ExpiresAtUtc,
                proposal.ResultEntityType,
                proposal.ResultEntityId))
            .ToList();
    }
}
