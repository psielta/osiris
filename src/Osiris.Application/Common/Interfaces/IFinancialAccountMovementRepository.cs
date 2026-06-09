using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface IFinancialAccountMovementRepository
{
    Task AddAsync(
        FinancialAccountMovement movement,
        FinancialAccount account,
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
}
