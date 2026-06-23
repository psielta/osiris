using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Commands.RejectAction;

public sealed class RejectActionCommandHandler : IRequestHandler<RejectActionCommand, Result>
{
    private readonly IAiActionProposalRepository _proposals;
    private readonly ICurrentUser _currentUser;

    public RejectActionCommandHandler(IAiActionProposalRepository proposals, ICurrentUser currentUser)
    {
        _proposals = proposals;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RejectActionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(new ResultError("Usuário não autenticado.", null, ResultErrorCodes.Unauthorized));
        }

        var proposal = await _proposals.GetAsync(_currentUser.TenantId, userId, request.Id, cancellationToken);
        if (proposal is null)
        {
            return Result.Failure(new ResultError("Proposta não encontrada.", null, ResultErrorCodes.NotFound));
        }

        // Idempotent: rejecting an already-rejected proposal succeeds.
        if (proposal.Status == AiActionProposalStatus.Rejected)
        {
            return Result.Success();
        }

        if (!proposal.IsPending)
        {
            return Result.Failure(new ResultError("Esta proposta já foi resolvida.", null, ResultErrorCodes.Conflict));
        }

        proposal.Reject();
        await _proposals.UpdateAsync(proposal, cancellationToken);
        return Result.Success();
    }
}
