using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class FinancialCategoryConfiguration : IEntityTypeConfiguration<FinancialCategory>
{
    public void Configure(EntityTypeBuilder<FinancialCategory> builder)
    {
        builder.ToTable("FinancialCategories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.TenantId)
            .IsRequired();

        builder.Property(category => category.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(category => category.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(category => category.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(category => category.Color)
            .HasMaxLength(7);

        builder.Property(category => category.IsActive)
            .IsRequired();

        builder.Property(category => category.CreatedAtUtc)
            .IsRequired();

        builder.Property(category => category.UpdatedAtUtc);

        builder.HasIndex(category => category.TenantId);

        builder.HasIndex(category => new { category.TenantId, category.NormalizedName, category.Type })
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(category => category.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
