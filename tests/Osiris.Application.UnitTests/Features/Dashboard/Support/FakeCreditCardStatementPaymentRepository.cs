using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.Dashboard.Support;

internal sealed class FakeCreditCardStatementPaymentRepository : ICreditCardStatementPaymentRepository
{
    private readonly List<CreditCardStatementPayment> _payments = new();

    public IReadOnlyList<CreditCardStatementPayment> Payments => _payments;

    public Task AddAsync(
        CreditCardStatementPayment payment,
        CreditCardStatement statement,
        FinancialAccountMovement? movement,
        FinancialAccount? account,
        CancellationToken cancellationToken)
    {
        _payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<CreditCardStatementPayment>> ListByStatementAsync(
        Guid tenantId,
        Guid creditCardStatementId,
        CancellationToken cancellationToken)
    {
        var payments = _payments
            .Where(payment => payment.TenantId == tenantId
                && payment.CreditCardStatementId == creditCardStatementId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardStatementPayment>>(payments);
    }

    public Task<IReadOnlyCollection<CreditCardStatementPayment>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var payments = _payments
            .Where(payment => payment.TenantId == tenantId
                && payment.PaidAt >= monthStart
                && payment.PaidAt < nextMonthStart)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardStatementPayment>>(payments);
    }

    public void Add(CreditCardStatementPayment payment)
    {
        _payments.Add(payment);
    }
}
