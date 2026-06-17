using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases.Support;

internal sealed class FakeCreditCardStatementRepository : ICreditCardStatementRepository
{
    private readonly List<CreditCardStatement> _statements = new();
    private readonly FakeCreditCardInstallmentRepository _installmentStore;

    public FakeCreditCardStatementRepository(FakeCreditCardInstallmentRepository installmentStore)
    {
        _installmentStore = installmentStore;
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
            .OrderByDescending(statement => statement.ReferenceYear)
            .ThenByDescending(statement => statement.ReferenceMonth)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardStatement>>(statements);
    }

    public Task<IReadOnlyCollection<CreditCardStatement>> ListAsync(
        Guid tenantId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = _statements.Where(statement => statement.TenantId == tenantId);

        if (from.HasValue)
        {
            query = query.Where(statement => statement.DueDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(statement => statement.DueDate <= to.Value);
        }

        var statements = query.OrderBy(statement => statement.DueDate).ToArray();

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
                    _installmentStore.Installments
                        .Where(installment => installment.TenantId == tenantId
                            && installment.CreditCardStatementId == id)
                        .Sum(installment => installment.Amount),
                    0m));

        return Task.FromResult<IReadOnlyDictionary<Guid, CreditCardStatementTotals>>(totals);
    }

    public Task UpdateAsync(CreditCardStatement statement, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Add(CreditCardStatement statement)
    {
        _statements.Add(statement);
    }

    public void AddRange(IEnumerable<CreditCardStatement> statements)
    {
        _statements.AddRange(statements);
    }
}
