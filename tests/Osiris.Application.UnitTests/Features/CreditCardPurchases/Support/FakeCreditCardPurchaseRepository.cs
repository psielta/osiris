using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases.Support;

internal sealed class FakeCreditCardPurchaseRepository : ICreditCardPurchaseRepository
{
    private readonly List<CreditCardPurchase> _purchases = new();
    private readonly FakeCreditCardInstallmentRepository _installmentStore;
    private readonly FakeCreditCardStatementRepository _statementStore;

    public FakeCreditCardPurchaseRepository(
        FakeCreditCardInstallmentRepository installmentStore,
        FakeCreditCardStatementRepository statementStore)
    {
        _installmentStore = installmentStore;
        _statementStore = statementStore;
    }

    public IReadOnlyList<CreditCardPurchase> Purchases => _purchases;

    public Task AddAsync(
        CreditCardPurchase purchase,
        IReadOnlyCollection<CreditCardInstallment> installments,
        IReadOnlyCollection<CreditCardStatement> newStatements,
        CancellationToken cancellationToken)
    {
        _statementStore.AddRange(newStatements);
        _purchases.Add(purchase);
        _installmentStore.AddRange(installments);
        return Task.CompletedTask;
    }

    public Task<CreditCardPurchase?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var purchase = _purchases.SingleOrDefault(purchase =>
            purchase.TenantId == tenantId && purchase.Id == id);

        return Task.FromResult(purchase);
    }

    public Task<IReadOnlyCollection<CreditCardPurchase>> ListByCardAsync(
        Guid tenantId,
        Guid creditCardId,
        CancellationToken cancellationToken)
    {
        var purchases = _purchases
            .Where(purchase => purchase.TenantId == tenantId && purchase.CreditCardId == creditCardId)
            .OrderByDescending(purchase => purchase.PurchaseDate)
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
        CancellationToken cancellationToken)
    {
        var purchases = _purchases
            .Where(purchase => purchase.TenantId == tenantId)
            .OrderByDescending(purchase => purchase.PurchaseDate)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardPurchase>>(purchases);
    }

    public Task DeleteAsync(
        CreditCardPurchase purchase,
        IReadOnlyCollection<CreditCardInstallment> installments,
        CancellationToken cancellationToken)
    {
        _installmentStore.RemoveRange(installments);
        _purchases.Remove(purchase);
        return Task.CompletedTask;
    }

    public void Add(CreditCardPurchase purchase)
    {
        _purchases.Add(purchase);
    }
}
