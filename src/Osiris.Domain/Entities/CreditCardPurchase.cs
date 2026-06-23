using Osiris.Domain.Common;

namespace Osiris.Domain.Entities;

public sealed class CreditCardPurchase : BaseEntity
{
    private CreditCardPurchase() { }

    public CreditCardPurchase(
        Guid tenantId,
        Guid creditCardId,
        Guid categoryId,
        string description,
        decimal totalAmount,
        DateOnly purchaseDate,
        int installments,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (creditCardId == Guid.Empty)
        {
            throw new ArgumentException("Credit card id is required.", nameof(creditCardId));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category id is required.", nameof(categoryId));
        }

        if (totalAmount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Total amount must be positive.");
        }

        if (installments < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(installments), "Installments must be greater than or equal to 1.");
        }

        TenantId = tenantId;
        CreditCardId = creditCardId;
        CategoryId = categoryId;
        Description = NormalizeRequiredDescription(description);
        TotalAmount = totalAmount;
        PurchaseDate = purchaseDate;
        Installments = installments;
        Notes = NormalizeOptional(notes);
    }

    public Guid TenantId { get; private set; }
    public Guid CreditCardId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public DateOnly PurchaseDate { get; private set; }
    public int Installments { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Reclassifies the purchase under a different expense category. This only changes how the purchase
    /// is grouped in spending reports; it does not touch amounts, installments or statements.
    /// </summary>
    public void ChangeCategory(Guid categoryId, DateTime utcNow)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category id is required.", nameof(categoryId));
        }

        CategoryId = categoryId;
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
