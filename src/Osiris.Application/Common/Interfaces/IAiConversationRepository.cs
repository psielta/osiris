using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

/// <summary>
/// Persistence for assistant conversations, always scoped to one tenant and one user. A conversation
/// owned by another user or tenant must read as "not found" — never leak across the boundary.
/// </summary>
public interface IAiConversationRepository
{
    Task<AiConversation?> GetAsync(Guid tenantId, string userId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AiMessage>> ListMessagesAsync(
        Guid tenantId,
        Guid conversationId,
        int maxMessages,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists a completed turn in one unit of work: inserts the conversation when new (otherwise
    /// touches it) and adds the new messages and tool-call audit rows together.
    /// </summary>
    Task SaveTurnAsync(
        AiConversation conversation,
        bool isNewConversation,
        IReadOnlyList<AiMessage> newMessages,
        IReadOnlyList<AiToolCall> toolCalls,
        CancellationToken cancellationToken);
}
