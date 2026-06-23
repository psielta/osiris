using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Infrastructure.Persistence;

public sealed class AiActionProposalRepository : IAiActionProposalRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AiActionProposalRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        await _dbContext.AiActionProposals.AddAsync(proposal, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<AiActionProposal?> GetAsync(Guid tenantId, string userId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.AiActionProposals
            .SingleOrDefaultAsync(
                proposal => proposal.TenantId == tenantId && proposal.UserId == userId && proposal.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<AiActionProposal>> ListPendingForConversationAsync(
        Guid tenantId,
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AiActionProposals
            .Where(proposal => proposal.TenantId == tenantId
                && proposal.UserId == userId
                && proposal.ConversationId == conversationId
                && proposal.Status == AiActionProposalStatus.Pending)
            .OrderBy(proposal => proposal.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        _dbContext.AiActionProposals.Update(proposal);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
