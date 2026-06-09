using Osiris.Domain.Common;

namespace Osiris.Domain.Entities;

public sealed class CreditCardStatementPayment : BaseEntity
{
    private CreditCardStatementPayment() { }

    public CreditCardStatementPayment(
        Guid tenantId,
        Guid creditCardStatementId,
        Guid? financialAccountId,
        decimal amount,
        DateOnly paidAt,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (creditCardStatementId == Guid.Empty)
        {
            throw new ArgumentException("Credit card statement id is required.", nameof(creditCardStatementId));
        }

        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        TenantId = tenantId;
        CreditCardStatementId = creditCardStatementId;
        FinancialAccountId = financialAccountId;
        Amount = amount;
        PaidAt = paidAt;
        Notes = NormalizeOptional(notes);
    }

    public Guid TenantId { get; private set; }
    public Guid CreditCardStatementId { get; private set; }
    public Guid? FinancialAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly PaidAt { get; private set; }
    public string? Notes { get; private set; }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
