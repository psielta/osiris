using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.Dashboard.Support;

internal sealed class FakeCreditCardRepository : ICreditCardRepository
{
    private readonly List<CreditCard> _cards = new();

    public Task<bool> ExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_cards.Any(card =>
            card.TenantId == tenantId
            && card.NormalizedName == normalizedName
            && card.Id != excludeId));
    }

    public Task<CreditCard?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_cards.SingleOrDefault(card => card.TenantId == tenantId && card.Id == id));
    }

    public Task<IReadOnlyCollection<CreditCard>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var cards = _cards
            .Where(card => card.TenantId == tenantId && (includeArchived || card.IsActive))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCard>>(cards);
    }

    public Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken)
    {
        _cards.Add(creditCard);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CreditCard creditCard, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Add(CreditCard creditCard)
    {
        _cards.Add(creditCard);
    }
}
