using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.AiAssistant.Commands.ArchiveConversation;

public sealed class ArchiveConversationCommandHandler : IRequestHandler<ArchiveConversationCommand, Result>
{
    private readonly IAiConversationRepository _conversations;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveConversationCommandHandler(
        IAiConversationRepository conversations,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _conversations = conversations;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveConversationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(new ResultError("Usuário não autenticado.", null, ResultErrorCodes.Unauthorized));
        }

        var conversation = await _conversations.GetAsync(
            _currentUser.TenantId,
            userId,
            request.Id,
            cancellationToken);

        if (conversation is null)
        {
            return Result.Failure(new ResultError("Conversa não encontrada.", null, ResultErrorCodes.NotFound));
        }

        conversation.Archive(_dateTimeProvider.UtcNow);
        await _conversations.UpdateAsync(conversation, cancellationToken);

        return Result.Success();
    }
}
