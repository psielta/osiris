using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class CreditCardPurchaseConfiguration : IEntityTypeConfiguration<CreditCardPurchase>
{
    public void Configure(EntityTypeBuilder<CreditCardPurchase> builder)
    {
        builder.ToTable("CreditCardPurchases");

        builder.HasKey(purchase => purchase.Id);

        builder.Property(purchase => purchase.TenantId)
            .IsRequired();

        builder.Property(purchase => purchase.CreditCardId)
            .IsRequired();

        // CategoryId is a soft reference (no FK), matching FinancialAccountMovement.CategoryId:
        // same-tenant ownership and the Expense type are enforced in the application handlers.
        builder.Property(purchase => purchase.CategoryId)
            .IsRequired();

        builder.Property(purchase => purchase.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(purchase => purchase.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(purchase => purchase.PurchaseDate)
            .IsRequired();

        builder.Property(purchase => purchase.Installments)
            .IsRequired();

        builder.Property(purchase => purchase.Notes)
            .HasMaxLength(500);

        builder.Property(purchase => purchase.CreatedAtUtc)
            .IsRequired();

        builder.Property(purchase => purchase.UpdatedAtUtc);

        builder.HasIndex(purchase => new { purchase.TenantId, purchase.CreditCardId });

        builder.HasIndex(purchase => new { purchase.TenantId, purchase.PurchaseDate });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(purchase => purchase.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CreditCard>()
            .WithMany()
            .HasForeignKey(purchase => purchase.CreditCardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
