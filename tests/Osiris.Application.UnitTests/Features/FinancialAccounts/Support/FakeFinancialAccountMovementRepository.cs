using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.FinancialAccounts.Support;

internal sealed class FakeFinancialAccountMovementRepository : IFinancialAccountMovementRepository
{
    private readonly List<FinancialAccountMovement> _movements = new();

    public IReadOnlyList<FinancialAccountMovement> Movements => _movements;

    public Task AddAsync(
        FinancialAccountMovement movement,
        FinancialAccount account,
        CancellationToken cancellationToken)
    {
        _movements.Add(movement);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<FinancialAccountMovement>> ListByAccountAsync(
        Guid tenantId,
        Guid financialAccountId,
        CancellationToken cancellationToken)
    {
        var movements = _movements
            .Where(movement => movement.TenantId == tenantId && movement.FinancialAccountId == financialAccountId)
            .OrderByDescending(movement => movement.OccurredOn)
            .ThenByDescending(movement => movement.CreatedAtUtc)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<FinancialAccountMovement>>(movements);
    }
}
