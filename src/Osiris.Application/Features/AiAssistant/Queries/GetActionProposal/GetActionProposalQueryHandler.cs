using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Application.Features.AiAssistant.Queries.GetActionProposal;

public sealed class GetActionProposalQueryHandler : IRequestHandler<GetActionProposalQuery, AiActionProposalDto?>
{
    private readonly IAiActionProposalRepository _proposals;
    private readonly ICurrentUser _currentUser;

    public GetActionProposalQueryHandler(IAiActionProposalRepository proposals, ICurrentUser currentUser)
    {
        _proposals = proposals;
        _currentUser = currentUser;
    }

    public async Task<AiActionProposalDto?> Handle(GetActionProposalQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var proposal = await _proposals.GetAsync(_currentUser.TenantId, userId, request.Id, cancellationToken);
        if (proposal is null)
        {
            return null;
        }

        return new AiActionProposalDto(
            proposal.Id,
            proposal.ActionType,
            proposal.DisplaySummary,
            proposal.ImpactSummary,
            proposal.RiskLevel.ToString(),
            proposal.Status.ToString(),
            proposal.CreatedAtUtc,
            proposal.ExpiresAtUtc,
            proposal.ResultEntityType,
            proposal.ResultEntityId);
    }
}
