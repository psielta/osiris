using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.AiAssistant.Commands.DeleteConversation;

public sealed class DeleteConversationCommandHandler : IRequestHandler<DeleteConversationCommand, Result>
{
    private readonly IAiConversationRepository _conversations;
    private readonly ICurrentUser _currentUser;

    public DeleteConversationCommandHandler(IAiConversationRepository conversations, ICurrentUser currentUser)
    {
        _conversations = conversations;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(new ResultError("Usuário não autenticado.", null, ResultErrorCodes.Unauthorized));
        }

        var conversation = await _conversations.GetAsync(_currentUser.TenantId, userId, request.Id, cancellationToken);
        if (conversation is null)
        {
            return Result.Failure(new ResultError("Conversa não encontrada.", null, ResultErrorCodes.NotFound));
        }

        await _conversations.DeleteAsync(conversation, cancellationToken);
        return Result.Success();
    }
}
