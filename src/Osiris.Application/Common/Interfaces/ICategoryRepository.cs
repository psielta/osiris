using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Common.Interfaces;

public interface ICategoryRepository
{
    Task<bool> ExistsAsync(
        Guid tenantId,
        string normalizedName,
        CategoryType type,
        Guid? excludeId,
        CancellationToken cancellationToken);

    Task<FinancialCategory?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FinancialCategory>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken);

    Task AddAsync(FinancialCategory category, CancellationToken cancellationToken);

    Task UpdateAsync(FinancialCategory category, CancellationToken cancellationToken);

    Task DeleteAsync(FinancialCategory category, CancellationToken cancellationToken);
}
