using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.CreditCardStatementPayments.Support;

internal sealed class FakeCreditCardStatementRepository : ICreditCardStatementRepository
{
    private readonly List<CreditCardStatement> _statements = new();
    private readonly Dictionary<Guid, decimal> _installmentTotals = new();
    private readonly FakeCreditCardStatementPaymentRepository _paymentStore;

    public FakeCreditCardStatementRepository(FakeCreditCardStatementPaymentRepository paymentStore)
    {
        _paymentStore = paymentStore;
    }

    public IReadOnlyList<CreditCardStatement> Statements => _statements;

    public Task<CreditCardStatement?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var statement = _statements.SingleOrDefault(statement =>
            statement.TenantId == tenantId && statement.Id == id);

        return Task.FromResult(statement);
    }

    public Task<CreditCardStatement?> GetByReferenceAsync(
        Guid tenantId,
        Guid creditCardId,
        int referenceYear,
        int referenceMonth,
        CancellationToken cancellationToken)
    {
        var statement = _statements.SingleOrDefault(statement =>
            statement.TenantId == tenantId
            && statement.CreditCardId == creditCardId
            && statement.ReferenceYear == referenceYear
            && statement.ReferenceMonth == referenceMonth);

        return Task.FromResult(statement);
    }

    public Task<IReadOnlyCollection<CreditCardStatement>> ListByCardAsync(
        Guid tenantId,
        Guid creditCardId,
        CancellationToken cancellationToken)
    {
        var statements = _statements
            .Where(statement => statement.TenantId == tenantId && statement.CreditCardId == creditCardId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardStatement>>(statements);
    }

    public Task<IReadOnlyCollection<CreditCardStatement>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var statements = _statements
            .Where(statement => statement.TenantId == tenantId)
            .OrderBy(statement => statement.DueDate)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardStatement>>(statements);
    }

    public Task<IReadOnlyCollection<CreditCardStatement>> ListByIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        var statements = _statements
            .Where(statement => statement.TenantId == tenantId && ids.Contains(statement.Id))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardStatement>>(statements);
    }

    public Task<IReadOnlyDictionary<Guid, CreditCardStatementTotals>> GetTotalsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> statementIds,
        CancellationToken cancellationToken)
    {
        var totals = statementIds
            .Distinct()
            .ToDictionary(
                id => id,
                id => new CreditCardStatementTotals(
                    _installmentTotals.GetValueOrDefault(id),
                    _paymentStore.Payments
                        .Where(payment => payment.TenantId == tenantId && payment.CreditCardStatementId == id)
                        .Sum(payment => payment.Amount)));

        return Task.FromResult<IReadOnlyDictionary<Guid, CreditCardStatementTotals>>(totals);
    }

    public Task UpdateAsync(CreditCardStatement statement, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Add(CreditCardStatement statement, decimal installmentsTotal)
    {
        _statements.Add(statement);
        _installmentTotals[statement.Id] = installmentsTotal;
    }
}
