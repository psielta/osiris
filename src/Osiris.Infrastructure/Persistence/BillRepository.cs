using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class BillRepository : IBillRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BillRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Bill?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Bills
            .SingleOrDefaultAsync(bill => bill.TenantId == tenantId && bill.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Bill>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        return await _dbContext.Bills
            .Where(bill => bill.TenantId == tenantId
                && bill.DueDate >= monthStart
                && bill.DueDate < nextMonthStart)
            .OrderBy(bill => bill.DueDate)
            .ThenBy(bill => bill.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(Bill bill, CancellationToken cancellationToken)
    {
        await _dbContext.Bills.AddAsync(bill, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Bill bill, CancellationToken cancellationToken)
    {
        _dbContext.Bills.Update(bill);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveStatusChangeAsync(
        Bill bill,
        FinancialAccountMovement? movementToAdd,
        FinancialAccountMovement? movementToRemove,
        FinancialAccount? account,
        CancellationToken cancellationToken)
    {
        _dbContext.Bills.Update(bill);

        if (movementToAdd is not null)
        {
            await _dbContext.FinancialAccountMovements.AddAsync(movementToAdd, cancellationToken);
        }

        if (movementToRemove is not null)
        {
            _dbContext.FinancialAccountMovements.Remove(movementToRemove);
        }

        if (account is not null)
        {
            _dbContext.FinancialAccounts.Update(account);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Bill bill,
        FinancialAccountMovement? movementToRemove,
        FinancialAccount? account,
        CancellationToken cancellationToken)
    {
        _dbContext.Bills.Remove(bill);

        if (movementToRemove is not null)
        {
            _dbContext.FinancialAccountMovements.Remove(movementToRemove);
        }

        if (account is not null)
        {
            _dbContext.FinancialAccounts.Update(account);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
