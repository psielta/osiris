using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant.Support;

internal sealed class FakeAiActionProposalRepository : IAiActionProposalRepository
{
    private readonly List<AiActionProposal> _proposals = new();

    public FakeAiActionProposalRepository(params AiActionProposal[] seed) => _proposals.AddRange(seed);

    public IReadOnlyList<AiActionProposal> Proposals => _proposals;

    public int UpdateCount { get; private set; }

    public Task AddAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        _proposals.Add(proposal);
        return Task.CompletedTask;
    }

    public Task<AiActionProposal?> GetAsync(Guid tenantId, string userId, Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_proposals.FirstOrDefault(proposal =>
            proposal.TenantId == tenantId && proposal.UserId == userId && proposal.Id == id));

    public Task<IReadOnlyList<AiActionProposal>> ListPendingForConversationAsync(
        Guid tenantId,
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AiActionProposal> pending = _proposals
            .Where(proposal => proposal.TenantId == tenantId
                && proposal.UserId == userId
                && proposal.ConversationId == conversationId
                && proposal.Status == AiActionProposalStatus.Pending)
            .ToList();
        return Task.FromResult(pending);
    }

    public Task UpdateAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        // The entity is mutated in place; just record that a save happened.
        UpdateCount++;
        return Task.CompletedTask;
    }
}
