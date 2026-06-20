using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class FinancialAccountMovementConfiguration : IEntityTypeConfiguration<FinancialAccountMovement>
{
    public void Configure(EntityTypeBuilder<FinancialAccountMovement> builder)
    {
        builder.ToTable("FinancialAccountMovements");

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.TenantId)
            .IsRequired();

        builder.Property(movement => movement.FinancialAccountId)
            .IsRequired();

        builder.Property(movement => movement.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(movement => movement.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(movement => movement.OccurredOn)
            .IsRequired();

        builder.Property(movement => movement.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(movement => movement.RelatedEntityType)
            .HasMaxLength(100);

        builder.Property(movement => movement.Notes)
            .HasMaxLength(500);

        builder.Property(movement => movement.ExternalId)
            .HasMaxLength(255);

        builder.Property(movement => movement.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(movement => movement.TenantId);

        builder.HasIndex(movement => movement.FinancialAccountId);

        builder.HasIndex(movement => new { movement.TenantId, movement.OccurredOn });

        // Idempotency for OFX import: an external transaction id is unique within an account.
        // Filtered so manual movements (ExternalId == null) are exempt from the constraint.
        builder.HasIndex(movement => new { movement.TenantId, movement.FinancialAccountId, movement.ExternalId })
            .IsUnique()
            .HasFilter("\"ExternalId\" IS NOT NULL");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(movement => movement.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinancialAccount>()
            .WithMany()
            .HasForeignKey(movement => movement.FinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
