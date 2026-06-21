using Microsoft.EntityFrameworkCore;
using Npgsql;
using Osiris.Application.Common.Exceptions;
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

    public async Task AddRangeAsync(
        IReadOnlyCollection<FinancialAccountMovement> movements,
        FinancialAccount account,
        CancellationToken cancellationToken)
    {
        await _dbContext.FinancialAccountMovements.AddRangeAsync(movements, cancellationToken);
        _dbContext.FinancialAccounts.Update(account);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The unique (TenantId, FinancialAccountId, ExternalId) index rejected a concurrently
            // imported transaction. Nothing was persisted; surface it as an already-imported signal.
            throw new DuplicateExternalIdException(exception);
        }
    }

    public async Task SaveImportAsync(
        IReadOnlyCollection<FinancialAccountMovement> newMovements,
        IReadOnlyCollection<FinancialAccountMovement> linkedMovements,
        FinancialAccount account,
        CancellationToken cancellationToken)
    {
        if (newMovements.Count > 0)
        {
            await _dbContext.FinancialAccountMovements.AddRangeAsync(newMovements, cancellationToken);
        }

        if (linkedMovements.Count > 0)
        {
            // Reconciled movements are already tracked (loaded via ListByIdsAsync); UpdateRange is a
            // defensive no-op that guarantees the stamped ExternalId/ReconciledAtUtc are persisted.
            _dbContext.FinancialAccountMovements.UpdateRange(linkedMovements);
        }

        _dbContext.FinancialAccounts.Update(account);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The unique (TenantId, FinancialAccountId, ExternalId) index rejected a concurrently
            // imported transaction (insert or reconcile race). Nothing was persisted.
            throw new DuplicateExternalIdException(exception);
        }
    }

    public async Task<IReadOnlyCollection<string>> ListExistingExternalIdsAsync(
        Guid tenantId,
        Guid financialAccountId,
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken)
    {
        if (externalIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.FinancialAccountMovements
            .Where(movement => movement.TenantId == tenantId
                && movement.FinancialAccountId == financialAccountId
                && movement.ExternalId != null
                && externalIds.Contains(movement.ExternalId))
            .Select(movement => movement.ExternalId!)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FinancialAccountMovement>> ListReconciliationCandidatesAsync(
        Guid tenantId,
        Guid financialAccountId,
        DateOnly fromInclusive,
        DateOnly toInclusive,
        CancellationToken cancellationToken)
    {
        return await _dbContext.FinancialAccountMovements
            .Where(movement => movement.TenantId == tenantId
                && movement.FinancialAccountId == financialAccountId
                && movement.ExternalId == null
                && movement.OccurredOn >= fromInclusive
                && movement.OccurredOn <= toInclusive)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FinancialAccountMovement>> ListByIdsAsync(
        Guid tenantId,
        Guid financialAccountId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.FinancialAccountMovements
            .Where(movement => movement.TenantId == tenantId
                && movement.FinancialAccountId == financialAccountId
                && ids.Contains(movement.Id))
            .ToArrayAsync(cancellationToken);
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
