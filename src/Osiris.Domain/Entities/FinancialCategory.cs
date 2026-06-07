using Osiris.Domain.Common;
using Osiris.Domain.Enums;

namespace Osiris.Domain.Entities;

public sealed class FinancialCategory : BaseEntity
{
    private FinancialCategory()
    {
    }

    public FinancialCategory(
        Guid tenantId,
        string name,
        CategoryType type,
        string? color = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        TenantId = tenantId;
        Name = NormalizeRequiredName(name);
        NormalizedName = NormalizeName(Name);
        Type = type;
        Color = NormalizeColor(color);
        IsActive = true;
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public CategoryType Type { get; private set; }

    public string? Color { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static string NormalizeName(string name)
    {
        return name.Trim().ToUpperInvariant();
    }

    public void Update(string name, CategoryType type, string? color, DateTime utcNow)
    {
        Name = NormalizeRequiredName(name);
        NormalizedName = NormalizeName(Name);
        Type = type;
        Color = NormalizeColor(color);
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
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? NormalizeColor(string? color)
    {
        return string.IsNullOrWhiteSpace(color) ? null : color.Trim();
    }
}
