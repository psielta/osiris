using Osiris.Domain.Common;
using Osiris.Domain.Enums;

namespace Osiris.Domain.Entities;

public sealed class FinancialAccountMovement : BaseEntity
{
    private FinancialAccountMovement() { }

    public FinancialAccountMovement(
        Guid tenantId,
        Guid financialAccountId,
        FinancialAccountMovementType type,
        decimal amount,
        DateOnly occurredOn,
        string description,
        Guid? categoryId = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        string? notes = null,
        string? externalId = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (financialAccountId == Guid.Empty)
        {
            throw new ArgumentException("Financial account id is required.", nameof(financialAccountId));
        }

        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        TenantId = tenantId;
        FinancialAccountId = financialAccountId;
        Type = type;
        Amount = amount;
        OccurredOn = occurredOn;
        Description = NormalizeRequiredDescription(description);
        CategoryId = categoryId;
        RelatedEntityType = NormalizeOptional(relatedEntityType);
        RelatedEntityId = relatedEntityId;
        Notes = NormalizeOptional(notes);
        ExternalId = NormalizeOptional(externalId);
    }

    public Guid TenantId { get; private set; }
    public Guid FinancialAccountId { get; private set; }
    public FinancialAccountMovementType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid? CategoryId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>
    /// Stable identifier of the source transaction when this movement was imported from an
    /// external statement (OFX <c>FITID</c>, or a synthesized key when the bank omits it).
    /// Null for movements created manually or by internal flows. Used to skip duplicates on re-import.
    /// </summary>
    public string? ExternalId { get; private set; }

    /// <summary>
    /// When set, this previously un-imported movement (typically a manual entry) was reconciled with
    /// an imported statement line: its <see cref="ExternalId"/> was stamped so future re-imports skip it.
    /// Null for movements that were never reconciled.
    /// </summary>
    public DateTime? ReconciledAtUtc { get; private set; }

    /// <summary>
    /// Links a previously un-imported (typically manual) movement to an imported statement line by
    /// stamping its <see cref="ExternalId"/>, so future re-imports deduplicate it. Does not change the
    /// account balance: the movement already moved the balance when it was created.
    /// </summary>
    public void LinkImportedExternalId(string externalId, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("External id is required.", nameof(externalId));
        }

        if (ExternalId is not null)
        {
            throw new InvalidOperationException("Movement is already linked to an imported transaction.");
        }

        ExternalId = externalId.Trim();
        ReconciledAtUtc = utcNow;
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
