using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("CreditCards");

        builder.HasKey(card => card.Id);

        builder.Property(card => card.TenantId)
            .IsRequired();

        builder.Property(card => card.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(card => card.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(card => card.Limit)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(card => card.ClosingDay)
            .IsRequired();

        builder.Property(card => card.DueDay)
            .IsRequired();

        // PaymentAccountId is a soft, optional reference (no FK): the column is a plain nullable uuid,
        // and same-tenant ownership is enforced in the application handlers.
        builder.Property(card => card.PaymentAccountId);

        builder.Property(card => card.IsActive)
            .IsRequired();

        builder.Property(card => card.CreatedAtUtc)
            .IsRequired();

        builder.Property(card => card.UpdatedAtUtc);

        builder.HasIndex(card => card.TenantId);

        builder.HasIndex(card => new { card.TenantId, card.NormalizedName })
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(card => card.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
