using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface ICsvImportPreferenceRepository
{
    Task<CsvImportPreference?> GetByAccountAsync(
        Guid tenantId,
        Guid financialAccountId,
        CancellationToken cancellationToken);

    Task AddAsync(CsvImportPreference preference, CancellationToken cancellationToken);

    Task UpdateAsync(CsvImportPreference preference, CancellationToken cancellationToken);
}
