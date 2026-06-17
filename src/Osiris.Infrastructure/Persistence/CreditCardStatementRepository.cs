using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class CreditCardStatementRepository : ICreditCardStatementRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CreditCardStatementRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CreditCardStatement?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.CreditCardStatements
            .SingleOrDefaultAsync(
                statement => statement.TenantId == tenantId && statement.Id == id,
                cancellationToken);
    }

    public Task<CreditCardStatement?> GetByReferenceAsync(
        Guid tenantId,
        Guid creditCardId,
        int referenceYear,
        int referenceMonth,
        CancellationToken cancellationToken)
    {
        return _dbContext.CreditCardStatements
            .SingleOrDefaultAsync(
                statement => statement.TenantId == tenantId
                    && statement.CreditCardId == creditCardId
                    && statement.ReferenceYear == referenceYear
                    && statement.ReferenceMonth == referenceMonth,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCardStatement>> ListByCardAsync(
        Guid tenantId,
        Guid creditCardId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CreditCardStatements
            .Where(statement => statement.TenantId == tenantId && statement.CreditCardId == creditCardId)
            .OrderByDescending(statement => statement.ReferenceYear)
            .ThenByDescending(statement => statement.ReferenceMonth)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCardStatement>> ListAsync(
        Guid tenantId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.CreditCardStatements
            .Where(statement => statement.TenantId == tenantId);

        if (from.HasValue)
        {
            query = query.Where(statement => statement.DueDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(statement => statement.DueDate <= to.Value);
        }

        return await query
            .OrderBy(statement => statement.DueDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCardStatement>> ListByIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CreditCardStatements
            .Where(statement => statement.TenantId == tenantId && ids.Contains(statement.Id))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, CreditCardStatementTotals>> GetTotalsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> statementIds,
        CancellationToken cancellationToken)
    {
        var installmentTotals = await _dbContext.CreditCardInstallments
            .Where(installment => installment.TenantId == tenantId
                && statementIds.Contains(installment.CreditCardStatementId))
            .GroupBy(installment => installment.CreditCardStatementId)
            .Select(group => new { StatementId = group.Key, Total = group.Sum(installment => installment.Amount) })
            .ToDictionaryAsync(entry => entry.StatementId, entry => entry.Total, cancellationToken);

        var paymentTotals = await _dbContext.CreditCardStatementPayments
            .Where(payment => payment.TenantId == tenantId
                && statementIds.Contains(payment.CreditCardStatementId))
            .GroupBy(payment => payment.CreditCardStatementId)
            .Select(group => new { StatementId = group.Key, Total = group.Sum(payment => payment.Amount) })
            .ToDictionaryAsync(entry => entry.StatementId, entry => entry.Total, cancellationToken);

        return statementIds
            .Distinct()
            .ToDictionary(
                id => id,
                id => new CreditCardStatementTotals(
                    installmentTotals.GetValueOrDefault(id),
                    paymentTotals.GetValueOrDefault(id)));
    }

    public async Task UpdateAsync(CreditCardStatement statement, CancellationToken cancellationToken)
    {
        _dbContext.CreditCardStatements.Update(statement);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
