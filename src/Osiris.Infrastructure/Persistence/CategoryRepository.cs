using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Infrastructure.Persistence;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CategoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(
        Guid tenantId,
        string normalizedName,
        CategoryType type,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.FinancialCategories
            .AnyAsync(category =>
                category.TenantId == tenantId
                && category.NormalizedName == normalizedName
                && category.Type == type
                && category.Id != excludeId,
                cancellationToken);
    }

    public Task<FinancialCategory?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.FinancialCategories
            .SingleOrDefaultAsync(
                category => category.TenantId == tenantId && category.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<FinancialCategory>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        return await _dbContext.FinancialCategories
            .Where(category => category.TenantId == tenantId)
            .Where(category => includeArchived || category.IsActive)
            .OrderByDescending(category => category.IsActive)
            .ThenBy(category => category.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(FinancialCategory category, CancellationToken cancellationToken)
    {
        await _dbContext.FinancialCategories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FinancialCategory category, CancellationToken cancellationToken)
    {
        _dbContext.FinancialCategories.Update(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(FinancialCategory category, CancellationToken cancellationToken)
    {
        _dbContext.FinancialCategories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
