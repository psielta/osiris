using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.FinancialAccounts.Support;

internal sealed class FakeCsvImportPreferenceRepository : ICsvImportPreferenceRepository
{
    private readonly List<CsvImportPreference> _preferences = new();

    public IReadOnlyList<CsvImportPreference> Preferences => _preferences;

    public Task<CsvImportPreference?> GetByAccountAsync(
        Guid tenantId,
        Guid financialAccountId,
        CancellationToken cancellationToken)
    {
        var preference = _preferences.SingleOrDefault(
            preference => preference.TenantId == tenantId && preference.FinancialAccountId == financialAccountId);

        return Task.FromResult(preference);
    }

    public Task AddAsync(CsvImportPreference preference, CancellationToken cancellationToken)
    {
        _preferences.Add(preference);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CsvImportPreference preference, CancellationToken cancellationToken)
    {
        // The entity is already in the list (tracked by reference); nothing else to do for the fake.
        return Task.CompletedTask;
    }

    public void Add(CsvImportPreference preference) => _preferences.Add(preference);
}
