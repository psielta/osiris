using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface IBillRepository
{
    Task<Bill?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Bill>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken);

    Task AddAsync(Bill bill, CancellationToken cancellationToken);

    Task UpdateAsync(Bill bill, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a paid/pending transition in a single transaction: the bill itself, an added
    /// movement (payment) or removed movement (back to pending), and the account balance update
    /// when an account is involved.
    /// </summary>
    Task SaveStatusChangeAsync(
        Bill bill,
        FinancialAccountMovement? movementToAdd,
        FinancialAccountMovement? movementToRemove,
        FinancialAccount? account,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes the bill and, when it was paid from an account, the related movement plus the
    /// account balance restore, in a single transaction.
    /// </summary>
    Task DeleteAsync(
        Bill bill,
        FinancialAccountMovement? movementToRemove,
        FinancialAccount? account,
        CancellationToken cancellationToken);
}
