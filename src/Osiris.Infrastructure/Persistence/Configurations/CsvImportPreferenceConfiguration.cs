using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class CsvImportPreferenceConfiguration : IEntityTypeConfiguration<CsvImportPreference>
{
    public void Configure(EntityTypeBuilder<CsvImportPreference> builder)
    {
        builder.ToTable("CsvImportPreferences");

        builder.HasKey(preference => preference.Id);

        builder.Property(preference => preference.TenantId)
            .IsRequired();

        builder.Property(preference => preference.FinancialAccountId)
            .IsRequired();

        // The mapping is an opaque serialized payload; jsonb keeps it queryable/portable on Postgres.
        builder.Property(preference => preference.Mapping)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(preference => preference.CreatedAtUtc)
            .IsRequired();

        // One remembered mapping per account.
        builder.HasIndex(preference => new { preference.TenantId, preference.FinancialAccountId })
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(preference => preference.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinancialAccount>()
            .WithMany()
            .HasForeignKey(preference => preference.FinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
