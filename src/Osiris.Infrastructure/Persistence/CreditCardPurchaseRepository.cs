using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class CreditCardPurchaseRepository : ICreditCardPurchaseRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CreditCardPurchaseRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        CreditCardPurchase purchase,
        IReadOnlyCollection<CreditCardInstallment> installments,
        IReadOnlyCollection<CreditCardStatement> newStatements,
        CancellationToken cancellationToken)
    {
        await _dbContext.CreditCardStatements.AddRangeAsync(newStatements, cancellationToken);
        await _dbContext.CreditCardPurchases.AddAsync(purchase, cancellationToken);
        await _dbContext.CreditCardInstallments.AddRangeAsync(installments, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<CreditCardPurchase?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.CreditCardPurchases
            .SingleOrDefaultAsync(
                purchase => purchase.TenantId == tenantId && purchase.Id == id,
                cancellationToken);
    }

    public async Task UpdateAsync(CreditCardPurchase purchase, CancellationToken cancellationToken)
    {
        _dbContext.CreditCardPurchases.Update(purchase);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCardPurchase>> ListByCardAsync(
        Guid tenantId,
        Guid creditCardId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CreditCardPurchases
            .Where(purchase => purchase.TenantId == tenantId && purchase.CreditCardId == creditCardId)
            .OrderByDescending(purchase => purchase.PurchaseDate)
            .ThenByDescending(purchase => purchase.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCardPurchase>> ListByIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CreditCardPurchases
            .Where(purchase => purchase.TenantId == tenantId && ids.Contains(purchase.Id))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCardPurchase>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        return await _dbContext.CreditCardPurchases
            .Where(purchase => purchase.TenantId == tenantId
                && purchase.PurchaseDate >= monthStart
                && purchase.PurchaseDate < nextMonthStart)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCardPurchase>> ListAsync(
        Guid tenantId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.CreditCardPurchases
            .Where(purchase => purchase.TenantId == tenantId);

        if (from.HasValue)
        {
            query = query.Where(purchase => purchase.PurchaseDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(purchase => purchase.PurchaseDate <= to.Value);
        }

        return await query
            .OrderByDescending(purchase => purchase.PurchaseDate)
            .ThenByDescending(purchase => purchase.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        CreditCardPurchase purchase,
        IReadOnlyCollection<CreditCardInstallment> installments,
        CancellationToken cancellationToken)
    {
        _dbContext.CreditCardInstallments.RemoveRange(installments);
        _dbContext.CreditCardPurchases.Remove(purchase);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
