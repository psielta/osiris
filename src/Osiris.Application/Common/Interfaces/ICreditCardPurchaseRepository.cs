using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface ICreditCardPurchaseRepository
{
    /// <summary>
    /// Persists the purchase, its installments, and any statements created for it in a single
    /// transaction. Status changes on already-tracked statements are saved together.
    /// </summary>
    Task AddAsync(
        CreditCardPurchase purchase,
        IReadOnlyCollection<CreditCardInstallment> installments,
        IReadOnlyCollection<CreditCardStatement> newStatements,
        CancellationToken cancellationToken);

    Task<CreditCardPurchase?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes to an existing purchase (e.g. a category reclassification). Does not touch
    /// installments or statements, since those are unaffected by metadata-only edits.
    /// </summary>
    Task UpdateAsync(CreditCardPurchase purchase, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCardPurchase>> ListByCardAsync(
        Guid tenantId,
        Guid creditCardId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCardPurchase>> ListByIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCardPurchase>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCardPurchase>> ListAsync(
        Guid tenantId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes the purchase and its installments in a single transaction. Status changes on
    /// already-tracked statements are saved together.
    /// </summary>
    Task DeleteAsync(
        CreditCardPurchase purchase,
        IReadOnlyCollection<CreditCardInstallment> installments,
        CancellationToken cancellationToken);
}
