using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.AiAssistant.DTOs;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Queries.GetConversation;

public sealed class GetConversationQueryHandler
    : IRequestHandler<GetConversationQuery, AiConversationDetailDto?>
{
    private const int MaxMessages = 200;

    private readonly IAiConversationRepository _conversations;
    private readonly ICurrentUser _currentUser;

    public GetConversationQueryHandler(IAiConversationRepository conversations, ICurrentUser currentUser)
    {
        _conversations = conversations;
        _currentUser = currentUser;
    }

    public async Task<AiConversationDetailDto?> Handle(GetConversationQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var tenantId = _currentUser.TenantId;
        var conversation = await _conversations.GetAsync(tenantId, userId, request.Id, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        var messages = await _conversations.ListMessagesAsync(tenantId, conversation.Id, MaxMessages, cancellationToken);

        var visible = messages
            .Where(message => message.Role is AiMessageRole.User or AiMessageRole.Assistant)
            .Select(message => new AiMessageDto(
                message.Id,
                message.Role == AiMessageRole.User ? "user" : "assistant",
                message.Content,
                message.CreatedAtUtc))
            .ToList();

        return new AiConversationDetailDto(
            conversation.Id,
            conversation.Title,
            conversation.Status.ToString(),
            visible);
    }
}
