using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class CreditCardStatementPaymentRepository : ICreditCardStatementPaymentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CreditCardStatementPaymentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        CreditCardStatementPayment payment,
        CreditCardStatement statement,
        FinancialAccountMovement? movement,
        FinancialAccount? account,
        CancellationToken cancellationToken)
    {
        await _dbContext.CreditCardStatementPayments.AddAsync(payment, cancellationToken);
        _dbContext.CreditCardStatements.Update(statement);

        if (movement is not null)
        {
            await _dbContext.FinancialAccountMovements.AddAsync(movement, cancellationToken);
        }

        if (account is not null)
        {
            _dbContext.FinancialAccounts.Update(account);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCardStatementPayment>> ListByStatementAsync(
        Guid tenantId,
        Guid creditCardStatementId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CreditCardStatementPayments
            .Where(payment => payment.TenantId == tenantId
                && payment.CreditCardStatementId == creditCardStatementId)
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCardStatementPayment>> ListByMonthAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        return await _dbContext.CreditCardStatementPayments
            .Where(payment => payment.TenantId == tenantId
                && payment.PaidAt >= monthStart
                && payment.PaidAt < nextMonthStart)
            .ToArrayAsync(cancellationToken);
    }
}
