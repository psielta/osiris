using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class FinancialAccountMovementRepository : IFinancialAccountMovementRepository
{
    private readonly ApplicationDbContext _dbContext;

    public FinancialAccountMovementRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        FinancialAccountMovement movement,
        FinancialAccount account,
        CancellationToken cancellationToken)
    {
        await _dbContext.FinancialAccountMovements.AddAsync(movement, cancellationToken);
        _dbContext.FinancialAccounts.Update(account);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FinancialAccountMovement>> ListByAccountAsync(
        Guid tenantId,
        Guid financialAccountId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.FinancialAccountMovements
            .Where(movement => movement.TenantId == tenantId && movement.FinancialAccountId == financialAccountId)
            .OrderByDescending(movement => movement.OccurredOn)
            .ThenByDescending(movement => movement.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<FinancialAccountMovement?> GetByRelatedEntityAsync(
        Guid tenantId,
        string relatedEntityType,
        Guid relatedEntityId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.FinancialAccountMovements
            .SingleOrDefaultAsync(
                movement => movement.TenantId == tenantId
                    && movement.RelatedEntityType == relatedEntityType
                    && movement.RelatedEntityId == relatedEntityId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<FinancialAccountMovement>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        return await _dbContext.FinancialAccountMovements
            .Where(movement => movement.TenantId == tenantId
                && movement.OccurredOn >= monthStart
                && movement.OccurredOn < nextMonthStart)
            .ToArrayAsync(cancellationToken);
    }
}
