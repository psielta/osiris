using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.Dashboard.Support;

internal sealed class FakeCreditCardPurchaseRepository : ICreditCardPurchaseRepository
{
    private readonly List<CreditCardPurchase> _purchases = new();

    public Task AddAsync(
        CreditCardPurchase purchase,
        IReadOnlyCollection<CreditCardInstallment> installments,
        IReadOnlyCollection<CreditCardStatement> newStatements,
        CancellationToken cancellationToken)
    {
        _purchases.Add(purchase);
        return Task.CompletedTask;
    }

    public Task<CreditCardPurchase?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_purchases.SingleOrDefault(purchase =>
            purchase.TenantId == tenantId && purchase.Id == id));
    }

    public Task UpdateAsync(CreditCardPurchase purchase, CancellationToken cancellationToken)
    {
        // The entity is tracked by reference; in-place mutations are already reflected.
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<CreditCardPurchase>> ListByCardAsync(
        Guid tenantId,
        Guid creditCardId,
        CancellationToken cancellationToken)
    {
        var purchases = _purchases
            .Where(purchase => purchase.TenantId == tenantId && purchase.CreditCardId == creditCardId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardPurchase>>(purchases);
    }

    public Task<IReadOnlyCollection<CreditCardPurchase>> ListByIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        var purchases = _purchases
            .Where(purchase => purchase.TenantId == tenantId && ids.Contains(purchase.Id))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardPurchase>>(purchases);
    }

    public Task<IReadOnlyCollection<CreditCardPurchase>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var purchases = _purchases
            .Where(purchase => purchase.TenantId == tenantId
                && purchase.PurchaseDate >= monthStart
                && purchase.PurchaseDate < nextMonthStart)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardPurchase>>(purchases);
    }

    public Task<IReadOnlyCollection<CreditCardPurchase>> ListAsync(
        Guid tenantId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = _purchases.Where(purchase => purchase.TenantId == tenantId);

        if (from.HasValue)
        {
            query = query.Where(purchase => purchase.PurchaseDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(purchase => purchase.PurchaseDate <= to.Value);
        }

        var purchases = query.ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardPurchase>>(purchases);
    }

    public Task DeleteAsync(
        CreditCardPurchase purchase,
        IReadOnlyCollection<CreditCardInstallment> installments,
        CancellationToken cancellationToken)
    {
        _purchases.Remove(purchase);
        return Task.CompletedTask;
    }

    public void Add(CreditCardPurchase purchase)
    {
        _purchases.Add(purchase);
    }
}
