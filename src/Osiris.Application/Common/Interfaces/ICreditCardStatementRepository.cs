using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface ICreditCardStatementRepository
{
    Task<CreditCardStatement?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<CreditCardStatement?> GetByReferenceAsync(
        Guid tenantId,
        Guid creditCardId,
        int referenceYear,
        int referenceMonth,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCardStatement>> ListByCardAsync(
        Guid tenantId,
        Guid creditCardId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCardStatement>> ListAsync(
        Guid tenantId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCardStatement>> ListByIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns installment and payment totals for every requested statement id, including ids
    /// without rows yet (zeroed totals).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, CreditCardStatementTotals>> GetTotalsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> statementIds,
        CancellationToken cancellationToken);

    Task UpdateAsync(CreditCardStatement statement, CancellationToken cancellationToken);
}
