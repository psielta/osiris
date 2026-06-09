using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Bills.Support;

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    private readonly List<FinancialCategory> _categories = new();

    public Task<bool> ExistsAsync(
        Guid tenantId,
        string normalizedName,
        CategoryType type,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = _categories.Any(category =>
            category.TenantId == tenantId
            && category.NormalizedName == normalizedName
            && category.Type == type
            && category.Id != excludeId);

        return Task.FromResult(exists);
    }

    public Task<FinancialCategory?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var category = _categories.SingleOrDefault(category => category.TenantId == tenantId && category.Id == id);
        return Task.FromResult(category);
    }

    public Task<IReadOnlyCollection<FinancialCategory>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var categories = _categories
            .Where(category => category.TenantId == tenantId && (includeArchived || category.IsActive))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<FinancialCategory>>(categories);
    }

    public Task AddAsync(FinancialCategory category, CancellationToken cancellationToken)
    {
        _categories.Add(category);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FinancialCategory category, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FinancialCategory category, CancellationToken cancellationToken)
    {
        _categories.Remove(category);
        return Task.CompletedTask;
    }

    public void Add(FinancialCategory category)
    {
        _categories.Add(category);
    }
}
