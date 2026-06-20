using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class CsvImportPreferenceRepository : ICsvImportPreferenceRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CsvImportPreferenceRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CsvImportPreference?> GetByAccountAsync(
        Guid tenantId,
        Guid financialAccountId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CsvImportPreferences
            .SingleOrDefaultAsync(
                preference => preference.TenantId == tenantId
                    && preference.FinancialAccountId == financialAccountId,
                cancellationToken);
    }

    public async Task AddAsync(CsvImportPreference preference, CancellationToken cancellationToken)
    {
        await _dbContext.CsvImportPreferences.AddAsync(preference, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CsvImportPreference preference, CancellationToken cancellationToken)
    {
        _dbContext.CsvImportPreferences.Update(preference);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
