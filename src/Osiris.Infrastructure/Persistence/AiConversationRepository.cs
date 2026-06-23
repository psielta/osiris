using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

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

    public async Task<IReadOnlyList<AiConversation>> ListAsync(
        Guid tenantId,
        string userId,
        int maxConversations,
        CancellationToken cancellationToken)
    {
        var take = maxConversations <= 0 ? 50 : maxConversations;

        return await _dbContext.AiConversations
            .Where(conversation => conversation.TenantId == tenantId
                && conversation.UserId == userId
                && conversation.Status == AiConversationStatus.Active)
            .OrderByDescending(conversation => conversation.UpdatedAtUtc ?? conversation.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiConversation conversation, CancellationToken cancellationToken)
    {
        _dbContext.AiConversations.Update(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsync(AiConversation conversation, CancellationToken cancellationToken)
    {
        await _dbContext.AiConversations.AddAsync(conversation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> SumTokensSinceAsync(Guid tenantId, DateTime sinceUtc, CancellationToken cancellationToken)
    {
        var messages = _dbContext.AiMessages
            .Where(message => message.TenantId == tenantId && message.CreatedAtUtc >= sinceUtc);

        var inputTokens = await messages.SumAsync(message => (long)message.InputTokens, cancellationToken);
        var outputTokens = await messages.SumAsync(message => (long)message.OutputTokens, cancellationToken);

        return inputTokens + outputTokens;
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
        IReadOnlyList<AiMessage> newMessages,
        IReadOnlyList<AiToolCall> toolCalls,
        CancellationToken cancellationToken)
    {
        _dbContext.AiConversations.Update(conversation);

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
