using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface ICreditCardInstallmentRepository
{
    Task<IReadOnlyCollection<CreditCardInstallment>> ListByPurchaseAsync(
        Guid tenantId,
        Guid creditCardPurchaseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCardInstallment>> ListByStatementAsync(
        Guid tenantId,
        Guid creditCardStatementId,
        CancellationToken cancellationToken);
}
