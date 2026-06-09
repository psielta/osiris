using Osiris.Domain.Common;

namespace Osiris.Domain.Entities;

public sealed class CreditCard : BaseEntity
{
    private CreditCard() { }

    public CreditCard(
        Guid tenantId,
        string name,
        decimal limit,
        int closingDay,
        int dueDay,
        Guid? paymentAccountId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        TenantId = tenantId;
        Name = NormalizeRequiredName(name);
        NormalizedName = NormalizeName(Name);
        Limit = EnsureNonNegativeLimit(limit);
        ClosingDay = EnsureValidDay(closingDay, nameof(closingDay));
        DueDay = EnsureValidDay(dueDay, nameof(dueDay));
        PaymentAccountId = paymentAccountId;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public decimal Limit { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public Guid? PaymentAccountId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

    public void Update(
        string name,
        decimal limit,
        int closingDay,
        int dueDay,
        Guid? paymentAccountId,
        DateTime utcNow)
    {
        Name = NormalizeRequiredName(name);
        NormalizedName = NormalizeName(Name);
        Limit = EnsureNonNegativeLimit(limit);
        ClosingDay = EnsureValidDay(closingDay, nameof(closingDay));
        DueDay = EnsureValidDay(dueDay, nameof(dueDay));
        PaymentAccountId = paymentAccountId;
        UpdatedAtUtc = utcNow;
    }

    public void Archive(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeRequiredName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Credit card name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static decimal EnsureNonNegativeLimit(decimal limit)
    {
        if (limit < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than or equal to zero.");
        }

        return limit;
    }

    private static int EnsureValidDay(int day, string paramName)
    {
        if (day is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(paramName, "Day must be between 1 and 31.");
        }

        return day;
    }
}
