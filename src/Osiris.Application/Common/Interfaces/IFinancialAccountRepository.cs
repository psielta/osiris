using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface IFinancialAccountRepository
{
    Task<bool> ExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken);

    Task<FinancialAccount?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FinancialAccount>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken);

    Task AddAsync(FinancialAccount account, CancellationToken cancellationToken);

    Task UpdateAsync(FinancialAccount account, CancellationToken cancellationToken);
}
