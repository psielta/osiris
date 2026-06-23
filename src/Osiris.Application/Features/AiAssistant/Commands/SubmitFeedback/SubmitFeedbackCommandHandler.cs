using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.AiAssistant.Commands.SubmitFeedback;

public sealed class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, Result>
{
    private readonly IAiFeedbackRepository _feedback;
    private readonly ICurrentUser _currentUser;

    public SubmitFeedbackCommandHandler(IAiFeedbackRepository feedback, ICurrentUser currentUser)
    {
        _feedback = feedback;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(new ResultError("Usuário não autenticado.", null, ResultErrorCodes.Unauthorized));
        }

        var tenantId = _currentUser.TenantId;
        var exists = await _feedback.MessageExistsForUserAsync(tenantId, userId, request.MessageId, cancellationToken);
        if (!exists)
        {
            return Result.Failure(new ResultError("Mensagem não encontrada.", null, ResultErrorCodes.NotFound));
        }

        var feedback = new AiFeedback(
            tenantId,
            userId,
            request.MessageId,
            request.Rating,
            request.ReasonCode,
            request.Comment);

        await _feedback.AddAsync(feedback, cancellationToken);
        return Result.Success();
    }
}
