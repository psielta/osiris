using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.Dashboard.Support;

internal sealed class FakeFinancialAccountMovementRepository : IFinancialAccountMovementRepository
{
    private readonly List<FinancialAccountMovement> _movements = new();

    public Task AddAsync(
        FinancialAccountMovement movement,
        FinancialAccount account,
        CancellationToken cancellationToken)
    {
        _movements.Add(movement);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(
        IReadOnlyCollection<FinancialAccountMovement> movements,
        FinancialAccount account,
        CancellationToken cancellationToken)
    {
        _movements.AddRange(movements);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<string>> ListExistingExternalIdsAsync(
        Guid tenantId,
        Guid financialAccountId,
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken)
    {
        var existing = _movements
            .Where(movement => movement.TenantId == tenantId
                && movement.FinancialAccountId == financialAccountId
                && movement.ExternalId != null
                && externalIds.Contains(movement.ExternalId))
            .Select(movement => movement.ExternalId!)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<string>>(existing);
    }

    public Task<IReadOnlyCollection<FinancialAccountMovement>> ListByAccountAsync(
        Guid tenantId,
        Guid financialAccountId,
        CancellationToken cancellationToken)
    {
        var movements = _movements
            .Where(movement => movement.TenantId == tenantId
                && movement.FinancialAccountId == financialAccountId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<FinancialAccountMovement>>(movements);
    }

    public Task<FinancialAccountMovement?> GetByRelatedEntityAsync(
        Guid tenantId,
        string relatedEntityType,
        Guid relatedEntityId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_movements.SingleOrDefault(movement =>
            movement.TenantId == tenantId
            && movement.RelatedEntityType == relatedEntityType
            && movement.RelatedEntityId == relatedEntityId));
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

    public void Add(FinancialAccountMovement movement)
    {
        _movements.Add(movement);
    }
}
