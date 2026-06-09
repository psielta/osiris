using Osiris.Domain.Common;
using Osiris.Domain.Enums;

namespace Osiris.Domain.Entities;

/// <summary>
/// An off-card payment obligation (rent, utilities, tuition, subscriptions billed directly).
/// Credit card debt never becomes a Bill: statements remain <see cref="CreditCardStatement"/>.
/// </summary>
public sealed class Bill : BaseEntity
{
    private Bill() { }

    public Bill(
        Guid tenantId,
        Guid categoryId,
        string description,
        decimal amount,
        DateOnly dueDate,
        Guid? paymentAccountId = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category id is required.", nameof(categoryId));
        }

        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        TenantId = tenantId;
        CategoryId = categoryId;
        Description = NormalizeRequiredDescription(description);
        Amount = amount;
        DueDate = dueDate;
        PaymentAccountId = paymentAccountId;
        Notes = NormalizeOptional(notes);
    }

    public Guid TenantId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateOnly? PaidAt { get; private set; }
    public Guid? PaymentAccountId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsPaid => PaidAt is not null;

    /// <summary>
    /// Status is derived, never stored: a paid bill stays Paid regardless of dates, and an
    /// unpaid bill is Overdue only after the due date has fully passed.
    /// </summary>
    public static BillStatus CalculateStatus(DateOnly? paidAt, DateOnly dueDate, DateOnly today)
    {
        if (paidAt is not null)
        {
            return BillStatus.Paid;
        }

        return today > dueDate ? BillStatus.Overdue : BillStatus.Pending;
    }

    public BillStatus StatusOn(DateOnly today) => CalculateStatus(PaidAt, DueDate, today);

    public void Update(
        Guid categoryId,
        string description,
        decimal amount,
        DateOnly dueDate,
        Guid? paymentAccountId,
        string? notes,
        DateTime utcNow)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category id is required.", nameof(categoryId));
        }

        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        CategoryId = categoryId;
        Description = NormalizeRequiredDescription(description);
        Amount = amount;
        DueDate = dueDate;
        PaymentAccountId = paymentAccountId;
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Records the actual settlement: <paramref name="paymentAccountId"/> is the account the
    /// money left from (null when paid outside tracked accounts), replacing any planned account.
    /// </summary>
    public void MarkAsPaid(DateOnly paidAt, Guid? paymentAccountId, DateTime utcNow)
    {
        if (IsPaid)
        {
            throw new InvalidOperationException("Bill is already paid.");
        }

        PaidAt = paidAt;
        PaymentAccountId = paymentAccountId;
        UpdatedAtUtc = utcNow;
    }

    public void MarkAsPending(DateTime utcNow)
    {
        if (!IsPaid)
        {
            throw new InvalidOperationException("Bill is already pending.");
        }

        PaidAt = null;
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeRequiredDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        return description.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
