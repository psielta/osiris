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

    public Task<FinancialAccountMovement?> GetByRelatedEntityAsync(
        Guid tenantId,
        string relatedEntityType,
        Guid relatedEntityId,
        CancellationToken cancellationToken)
    {
        var movement = _movements.SingleOrDefault(movement =>
            movement.TenantId == tenantId
            && movement.RelatedEntityType == relatedEntityType
            && movement.RelatedEntityId == relatedEntityId);

        return Task.FromResult(movement);
    }

    public Task<IReadOnlyCollection<FinancialAccountMovement>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var movements = _movements
            .Where(movement => movement.TenantId == tenantId
                && movement.OccurredOn >= monthStart
                && movement.OccurredOn < nextMonthStart)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<FinancialAccountMovement>>(movements);
    }
}
