using Osiris.Domain.Common;

namespace Osiris.Domain.Entities;

/// <summary>
/// Remembers the last CSV column mapping a user applied when importing a statement into a given
/// account, so the next import can pre-fill the mapping form. One row per (tenant, account). The
/// mapping itself is an opaque serialized payload owned by the Application layer (stored as jsonb).
/// </summary>
public sealed class CsvImportPreference : BaseEntity
{
    private CsvImportPreference() { }

    public CsvImportPreference(Guid tenantId, Guid financialAccountId, string mapping)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (financialAccountId == Guid.Empty)
        {
            throw new ArgumentException("Financial account id is required.", nameof(financialAccountId));
        }

        TenantId = tenantId;
        FinancialAccountId = financialAccountId;
        Mapping = NormalizeRequired(mapping);
    }

    public Guid TenantId { get; private set; }
    public Guid FinancialAccountId { get; private set; }
    public string Mapping { get; private set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; private set; }

    public void Update(string mapping, DateTime utcNow)
    {
        Mapping = NormalizeRequired(mapping);
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeRequired(string mapping)
    {
        if (string.IsNullOrWhiteSpace(mapping))
        {
            throw new ArgumentException("Mapping payload is required.", nameof(mapping));
        }

        return mapping;
    }
}
