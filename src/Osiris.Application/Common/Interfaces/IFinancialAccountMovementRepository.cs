using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface IFinancialAccountMovementRepository
{
    Task AddAsync(
        FinancialAccountMovement movement,
        FinancialAccount account,
        CancellationToken cancellationToken);

    Task AddRangeAsync(
        IReadOnlyCollection<FinancialAccountMovement> movements,
        FinancialAccount account,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists an import in one unit of work: inserts <paramref name="newMovements"/> and saves the
    /// <paramref name="linkedMovements"/> that were reconciled (their external id was stamped). Translates
    /// the unique-index violation into <see cref="Exceptions.DuplicateExternalIdException"/>.
    /// </summary>
    Task SaveImportAsync(
        IReadOnlyCollection<FinancialAccountMovement> newMovements,
        IReadOnlyCollection<FinancialAccountMovement> linkedMovements,
        FinancialAccount account,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> ListExistingExternalIdsAsync(
        Guid tenantId,
        Guid financialAccountId,
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns un-imported movements (<c>ExternalId == null</c>) in the account within the inclusive date
    /// range, used as reconciliation candidates for an import preview.
    /// </summary>
    Task<IReadOnlyCollection<FinancialAccountMovement>> ListReconciliationCandidatesAsync(
        Guid tenantId,
        Guid financialAccountId,
        DateOnly fromInclusive,
        DateOnly toInclusive,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads the given movements scoped to the tenant and account (movements outside that scope are
    /// silently excluded), used to resolve reconciliation targets on import confirm.
    /// </summary>
    Task<IReadOnlyCollection<FinancialAccountMovement>> ListByIdsAsync(
        Guid tenantId,
        Guid financialAccountId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FinancialAccountMovement>> ListByAccountAsync(
        Guid tenantId,
        Guid financialAccountId,
        CancellationToken cancellationToken);

    Task<FinancialAccountMovement?> GetByRelatedEntityAsync(
        Guid tenantId,
        string relatedEntityType,
        Guid relatedEntityId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FinancialAccountMovement>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken);
}
