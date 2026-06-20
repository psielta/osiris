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

    Task<IReadOnlyCollection<string>> ListExistingExternalIdsAsync(
        Guid tenantId,
        Guid financialAccountId,
        IReadOnlyCollection<string> externalIds,
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
