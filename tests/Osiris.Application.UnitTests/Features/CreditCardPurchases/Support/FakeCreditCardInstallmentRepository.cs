using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases.Support;

internal sealed class FakeCreditCardInstallmentRepository : ICreditCardInstallmentRepository
{
    private readonly List<CreditCardInstallment> _installments = new();

    public IReadOnlyList<CreditCardInstallment> Installments => _installments;

    public Task<IReadOnlyCollection<CreditCardInstallment>> ListByPurchaseAsync(
        Guid tenantId,
        Guid creditCardPurchaseId,
        CancellationToken cancellationToken)
    {
        var installments = _installments
            .Where(installment => installment.TenantId == tenantId
                && installment.CreditCardPurchaseId == creditCardPurchaseId)
            .OrderBy(installment => installment.InstallmentNumber)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardInstallment>>(installments);
    }

    public Task<IReadOnlyCollection<CreditCardInstallment>> ListByStatementAsync(
        Guid tenantId,
        Guid creditCardStatementId,
        CancellationToken cancellationToken)
    {
        var installments = _installments
            .Where(installment => installment.TenantId == tenantId
                && installment.CreditCardStatementId == creditCardStatementId)
            .OrderBy(installment => installment.InstallmentNumber)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditCardInstallment>>(installments);
    }

    public void AddRange(IEnumerable<CreditCardInstallment> installments)
    {
        _installments.AddRange(installments);
    }

    public void RemoveRange(IEnumerable<CreditCardInstallment> installments)
    {
        foreach (var installment in installments.ToArray())
        {
            _installments.Remove(installment);
        }
    }
}
