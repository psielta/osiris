using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class FinancialAccountRepository : IFinancialAccountRepository
{
    private readonly ApplicationDbContext _dbContext;

    public FinancialAccountRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.FinancialAccounts
            .AnyAsync(account =>
                account.TenantId == tenantId
                && account.NormalizedName == normalizedName
                && account.Id != excludeId,
                cancellationToken);
    }

    public Task<FinancialAccount?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.FinancialAccounts
            .SingleOrDefaultAsync(
                account => account.TenantId == tenantId && account.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<FinancialAccount>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        return await _dbContext.FinancialAccounts
            .Where(account => account.TenantId == tenantId)
            .Where(account => includeArchived || account.IsActive)
            .OrderByDescending(account => account.IsActive)
            .ThenBy(account => account.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        await _dbContext.FinancialAccounts.AddAsync(account, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        _dbContext.FinancialAccounts.Update(account);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
