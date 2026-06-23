using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

/// <summary>
/// Persistence for write proposals, always scoped to one tenant and one user. A proposal owned by
/// another user/tenant must read as "not found".
/// </summary>
public interface IAiActionProposalRepository
{
    Task AddAsync(AiActionProposal proposal, CancellationToken cancellationToken);

    Task<AiActionProposal?> GetAsync(Guid tenantId, string userId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AiActionProposal>> ListPendingForConversationAsync(
        Guid tenantId,
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken);

    Task UpdateAsync(AiActionProposal proposal, CancellationToken cancellationToken);
}
