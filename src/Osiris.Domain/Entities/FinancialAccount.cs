using Osiris.Domain.Common;
using Osiris.Domain.Enums;

namespace Osiris.Domain.Entities;

public sealed class FinancialAccount : BaseEntity
{
    private FinancialAccount() { }

    public FinancialAccount(
        Guid tenantId,
        string name,
        FinancialAccountType type,
        decimal initialBalance)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        TenantId = tenantId;
        Name = NormalizeRequiredName(name);
        NormalizedName = NormalizeName(Name);
        Type = type;
        InitialBalance = initialBalance;
        CurrentBalance = initialBalance;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public FinancialAccountType Type { get; private set; }
    public decimal InitialBalance { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

    public void Update(string name, FinancialAccountType type, DateTime utcNow)
    {
        Name = NormalizeRequiredName(name);
        NormalizedName = NormalizeName(Name);
        Type = type;
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

    public void ApplyMovement(FinancialAccountMovementType type, decimal amount, DateTime utcNow)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        CurrentBalance += type.IsInflow() ? amount : -amount;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Undoes a previously applied movement when the movement record itself is being removed
    /// (e.g. a bill going back to pending); no new movement row should be created for this.
    /// </summary>
    public void RevertMovement(FinancialAccountMovementType type, decimal amount, DateTime utcNow)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        CurrentBalance += type.IsInflow() ? -amount : amount;
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeRequiredName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Account name is required.", nameof(name));
        }

        return name.Trim();
    }
}
