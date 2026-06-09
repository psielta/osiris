using Osiris.Domain.Common;

namespace Osiris.Domain.Entities;

public sealed class CreditCardInstallment : BaseEntity
{
    private CreditCardInstallment() { }

    public CreditCardInstallment(
        Guid tenantId,
        Guid creditCardPurchaseId,
        Guid creditCardStatementId,
        int installmentNumber,
        int totalInstallments,
        decimal amount,
        DateOnly dueDate)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (creditCardPurchaseId == Guid.Empty)
        {
            throw new ArgumentException("Credit card purchase id is required.", nameof(creditCardPurchaseId));
        }

        if (creditCardStatementId == Guid.Empty)
        {
            throw new ArgumentException("Credit card statement id is required.", nameof(creditCardStatementId));
        }

        if (totalInstallments < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalInstallments), "Total installments must be greater than or equal to 1.");
        }

        if (installmentNumber < 1 || installmentNumber > totalInstallments)
        {
            throw new ArgumentOutOfRangeException(nameof(installmentNumber), "Installment number must be between 1 and the total installments.");
        }

        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        TenantId = tenantId;
        CreditCardPurchaseId = creditCardPurchaseId;
        CreditCardStatementId = creditCardStatementId;
        InstallmentNumber = installmentNumber;
        TotalInstallments = totalInstallments;
        Amount = amount;
        DueDate = dueDate;
    }

    public Guid TenantId { get; private set; }
    public Guid CreditCardPurchaseId { get; private set; }
    public Guid CreditCardStatementId { get; private set; }
    public int InstallmentNumber { get; private set; }
    public int TotalInstallments { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly DueDate { get; private set; }
}
