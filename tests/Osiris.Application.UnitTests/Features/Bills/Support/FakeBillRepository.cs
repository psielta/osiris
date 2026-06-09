using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.Bills.Support;

internal sealed class FakeBillRepository : IBillRepository
{
    private readonly List<Bill> _bills = new();
    private readonly FakeFinancialAccountMovementRepository? _movementStore;

    public FakeBillRepository(FakeFinancialAccountMovementRepository? movementStore = null)
    {
        _movementStore = movementStore;
    }

    public IReadOnlyList<Bill> Bills => _bills;

    public Task<Bill?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var bill = _bills.SingleOrDefault(bill => bill.TenantId == tenantId && bill.Id == id);
        return Task.FromResult(bill);
    }

    public Task<IReadOnlyCollection<Bill>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var bills = _bills
            .Where(bill => bill.TenantId == tenantId
                && bill.DueDate >= monthStart
                && bill.DueDate < nextMonthStart)
            .OrderBy(bill => bill.DueDate)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Bill>>(bills);
    }

    public Task AddAsync(Bill bill, CancellationToken cancellationToken)
    {
        _bills.Add(bill);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Bill bill, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task SaveStatusChangeAsync(
        Bill bill,
        FinancialAccountMovement? movementToAdd,
        FinancialAccountMovement? movementToRemove,
        FinancialAccount? account,
        CancellationToken cancellationToken)
    {
        if (movementToAdd is not null)
        {
            _movementStore?.Add(movementToAdd);
        }

        if (movementToRemove is not null)
        {
            _movementStore?.Remove(movementToRemove);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Bill bill,
        FinancialAccountMovement? movementToRemove,
        FinancialAccount? account,
        CancellationToken cancellationToken)
    {
        _bills.Remove(bill);

        if (movementToRemove is not null)
        {
            _movementStore?.Remove(movementToRemove);
        }

        return Task.CompletedTask;
    }

    public void Add(Bill bill)
    {
        _bills.Add(bill);
    }
}
