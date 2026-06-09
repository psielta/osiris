using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface ICreditCardStatementPaymentRepository
{
    /// <summary>
    /// Persists the payment, the statement status change, and — when the payment comes out of a
    /// financial account — the account movement and updated balance, in a single transaction.
    /// </summary>
    Task AddAsync(
        CreditCardStatementPayment payment,
        CreditCardStatement statement,
        FinancialAccountMovement? movement,
        FinancialAccount? account,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCardStatementPayment>> ListByStatementAsync(
        Guid tenantId,
        Guid creditCardStatementId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCardStatementPayment>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken);
}
