using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Application.Features.AiAssistant.Queries.ListConversations;

public sealed class ListConversationsQueryHandler
    : IRequestHandler<ListConversationsQuery, IReadOnlyCollection<AiConversationListItemDto>>
{
    private const int MaxConversations = 50;

    private readonly IAiConversationRepository _conversations;
    private readonly ICurrentUser _currentUser;

    public ListConversationsQueryHandler(IAiConversationRepository conversations, ICurrentUser currentUser)
    {
        _conversations = conversations;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<AiConversationListItemDto>> Handle(
        ListConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Array.Empty<AiConversationListItemDto>();
        }

        var conversations = await _conversations.ListAsync(
            _currentUser.TenantId,
            userId,
            MaxConversations,
            cancellationToken);

        return conversations
            .Select(conversation => new AiConversationListItemDto(
                conversation.Id,
                conversation.Title,
                conversation.Status.ToString(),
                conversation.UpdatedAtUtc,
                conversation.CreatedAtUtc))
            .ToList();
    }
}
