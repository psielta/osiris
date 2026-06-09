using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.CreditCardStatementPayments.Support;

internal sealed class FakeCreditCardStatementPaymentRepository : ICreditCardStatementPaymentRepository
{
    private readonly List<CreditCardStatementPayment> _payments = new();
    private readonly List<FinancialAccountMovement> _movements = new();

    public IReadOnlyList<CreditCardStatementPayment> Payments => _payments;

    public IReadOnlyList<FinancialAccountMovement> Movements => _movements;

    public Task AddAsync(
        CreditCardStatementPayment payment,
        CreditCardStatement statement,
        FinancialAccountMovement? movement,
        FinancialAccount? account,
        CancellationToken cancellationToken)
    {
        _payments.Add(payment);
        if (movement is not null)
        {
            _movements.Add(movement);
        }

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

    public void Add(CreditCardStatementPayment payment)
    {
        _payments.Add(payment);
    }
}
