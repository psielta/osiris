using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases.Support;

internal sealed class FakeCreditCardRepository : ICreditCardRepository
{
    private readonly List<CreditCard> _creditCards = new();

    public Task<bool> ExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = _creditCards.Any(card =>
            card.TenantId == tenantId
            && card.NormalizedName == normalizedName
            && card.Id != excludeId);

        return Task.FromResult(exists);
    }

    public Task<CreditCard?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var card = _creditCards.SingleOrDefault(card => card.TenantId == tenantId && card.Id == id);
        return Task.FromResult(card);
    }

    public Task<IReadOnlyCollection<CreditCard>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var cards = _creditCards
            .Where(card => card.TenantId == tenantId && (includeArchived || card.IsActive))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCard>>(cards);
    }

    public Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken)
    {
        _creditCards.Add(creditCard);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CreditCard creditCard, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Add(CreditCard creditCard)
    {
        _creditCards.Add(creditCard);
    }
}
