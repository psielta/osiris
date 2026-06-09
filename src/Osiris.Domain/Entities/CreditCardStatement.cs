using Osiris.Domain.Common;
using Osiris.Domain.Enums;

namespace Osiris.Domain.Entities;

public sealed class CreditCardStatement : BaseEntity
{
    private CreditCardStatement() { }

    public CreditCardStatement(
        Guid tenantId,
        Guid creditCardId,
        int referenceMonth,
        int referenceYear,
        DateOnly closingDate,
        DateOnly dueDate)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (creditCardId == Guid.Empty)
        {
            throw new ArgumentException("Credit card id is required.", nameof(creditCardId));
        }

        if (referenceMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceMonth), "Reference month must be between 1 and 12.");
        }

        if (dueDate < closingDate)
        {
            throw new ArgumentException("Due date cannot be earlier than the closing date.", nameof(dueDate));
        }

        TenantId = tenantId;
        CreditCardId = creditCardId;
        ReferenceMonth = referenceMonth;
        ReferenceYear = referenceYear;
        ClosingDate = closingDate;
        DueDate = dueDate;
        Status = CreditCardStatementStatus.Open;
    }

    public Guid TenantId { get; private set; }
    public Guid CreditCardId { get; private set; }
    public int ReferenceMonth { get; private set; }
    public int ReferenceYear { get; private set; }
    public DateOnly ClosingDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public CreditCardStatementStatus Status { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Single source of truth for statement status. Paid wins over everything once the open
    /// balance reaches zero; an unpaid balance past the due date is Overdue even when partially
    /// paid; otherwise a payment in progress is PartiallyPaid, and date alone decides Open/Closed.
    /// </summary>
    public static CreditCardStatementStatus CalculateStatus(
        decimal totalAmount,
        decimal paidAmount,
        DateOnly closingDate,
        DateOnly dueDate,
        DateOnly today)
    {
        var openBalance = totalAmount - paidAmount;

        if (totalAmount > 0m && openBalance <= 0m)
        {
            return CreditCardStatementStatus.Paid;
        }

        if (openBalance > 0m && today > dueDate)
        {
            return CreditCardStatementStatus.Overdue;
        }

        if (openBalance > 0m && paidAmount > 0m)
        {
            return CreditCardStatementStatus.PartiallyPaid;
        }

        return today > closingDate
            ? CreditCardStatementStatus.Closed
            : CreditCardStatementStatus.Open;
    }

    public void RefreshStatus(decimal totalAmount, decimal paidAmount, DateOnly today, DateTime utcNow)
    {
        var status = CalculateStatus(totalAmount, paidAmount, ClosingDate, DueDate, today);
        if (status == Status)
        {
            return;
        }

        Status = status;
        UpdatedAtUtc = utcNow;
    }
}
