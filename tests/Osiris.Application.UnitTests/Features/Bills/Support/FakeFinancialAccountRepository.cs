using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.Bills.Support;

internal sealed class FakeFinancialAccountRepository : IFinancialAccountRepository
{
    private readonly List<FinancialAccount> _accounts = new();

    public Task<bool> ExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = _accounts.Any(account =>
            account.TenantId == tenantId
            && account.NormalizedName == normalizedName
            && account.Id != excludeId);

        return Task.FromResult(exists);
    }

    public Task<FinancialAccount?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var account = _accounts.SingleOrDefault(account => account.TenantId == tenantId && account.Id == id);
        return Task.FromResult(account);
    }

    public Task<IReadOnlyCollection<FinancialAccount>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var accounts = _accounts
            .Where(account => account.TenantId == tenantId && (includeArchived || account.IsActive))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<FinancialAccount>>(accounts);
    }

    public Task AddAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        _accounts.Add(account);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Add(FinancialAccount account)
    {
        _accounts.Add(account);
    }
}
