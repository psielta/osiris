using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class CreditCardInstallmentRepository : ICreditCardInstallmentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CreditCardInstallmentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CreditCardInstallment>> ListByPurchaseAsync(
        Guid tenantId,
        Guid creditCardPurchaseId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CreditCardInstallments
            .Where(installment => installment.TenantId == tenantId
                && installment.CreditCardPurchaseId == creditCardPurchaseId)
            .OrderBy(installment => installment.InstallmentNumber)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCardInstallment>> ListByStatementAsync(
        Guid tenantId,
        Guid creditCardStatementId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CreditCardInstallments
            .Where(installment => installment.TenantId == tenantId
                && installment.CreditCardStatementId == creditCardStatementId)
            .OrderBy(installment => installment.CreatedAtUtc)
            .ThenBy(installment => installment.InstallmentNumber)
            .ToArrayAsync(cancellationToken);
    }
}
