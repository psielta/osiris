using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("FinancialAccounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.TenantId)
            .IsRequired();

        builder.Property(account => account.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(account => account.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(account => account.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(account => account.InitialBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(account => account.CurrentBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(account => account.IsActive)
            .IsRequired();

        builder.Property(account => account.CreatedAtUtc)
            .IsRequired();

        builder.Property(account => account.UpdatedAtUtc);

        builder.HasIndex(account => account.TenantId);

        builder.HasIndex(account => new { account.TenantId, account.NormalizedName })
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(account => account.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
