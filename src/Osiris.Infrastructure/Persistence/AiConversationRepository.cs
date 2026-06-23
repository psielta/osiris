using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class AiConversationRepository : IAiConversationRepository
{
    private const int DefaultMaxMessages = 20;

    private readonly ApplicationDbContext _dbContext;

    public AiConversationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AiConversation?> GetAsync(Guid tenantId, string userId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.AiConversations
            .SingleOrDefaultAsync(
                conversation => conversation.TenantId == tenantId
                    && conversation.UserId == userId
                    && conversation.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<AiMessage>> ListMessagesAsync(
        Guid tenantId,
        Guid conversationId,
        int maxMessages,
        CancellationToken cancellationToken)
    {
        var take = maxMessages <= 0 ? DefaultMaxMessages : maxMessages;

        var recent = await _dbContext.AiMessages
            .Where(message => message.TenantId == tenantId && message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAtUtc)
            .ThenByDescending(message => message.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

        recent.Reverse();
        return recent;
    }

    public async Task SaveTurnAsync(
        AiConversation conversation,
        bool isNewConversation,
        IReadOnlyList<AiMessage> newMessages,
        IReadOnlyList<AiToolCall> toolCalls,
        CancellationToken cancellationToken)
    {
        if (isNewConversation)
        {
            await _dbContext.AiConversations.AddAsync(conversation, cancellationToken);
        }
        else
        {
            _dbContext.AiConversations.Update(conversation);
        }

        if (newMessages.Count > 0)
        {
            await _dbContext.AiMessages.AddRangeAsync(newMessages, cancellationToken);
        }

        if (toolCalls.Count > 0)
        {
            await _dbContext.AiToolCalls.AddRangeAsync(toolCalls, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
