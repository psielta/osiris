using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.Dashboard.Support;

internal sealed class FakeBillRepository : IBillRepository
{
    private readonly List<Bill> _bills = new();

    public Task<Bill?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_bills.SingleOrDefault(bill => bill.TenantId == tenantId && bill.Id == id));
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
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Bill>>(bills);
    }

    public Task<IReadOnlyCollection<Bill>> ListUnpaidAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var bills = _bills
            .Where(bill => bill.TenantId == tenantId && !bill.IsPaid)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Bill>>(bills);
    }

    public Task<IReadOnlyCollection<Bill>> ListPaidInMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var bills = _bills
            .Where(bill => bill.TenantId == tenantId
                && bill.PaidAt is not null
                && bill.PaidAt >= monthStart
                && bill.PaidAt < nextMonthStart)
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
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Bill bill,
        FinancialAccountMovement? movementToRemove,
        FinancialAccount? account,
        CancellationToken cancellationToken)
    {
        _bills.Remove(bill);
        return Task.CompletedTask;
    }

    public void Add(Bill bill)
    {
        _bills.Add(bill);
    }
}
