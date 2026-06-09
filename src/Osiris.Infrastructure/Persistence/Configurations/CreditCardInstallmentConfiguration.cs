using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class CreditCardInstallmentConfiguration : IEntityTypeConfiguration<CreditCardInstallment>
{
    public void Configure(EntityTypeBuilder<CreditCardInstallment> builder)
    {
        builder.ToTable("CreditCardInstallments");

        builder.HasKey(installment => installment.Id);

        builder.Property(installment => installment.TenantId)
            .IsRequired();

        builder.Property(installment => installment.CreditCardPurchaseId)
            .IsRequired();

        builder.Property(installment => installment.CreditCardStatementId)
            .IsRequired();

        builder.Property(installment => installment.InstallmentNumber)
            .IsRequired();

        builder.Property(installment => installment.TotalInstallments)
            .IsRequired();

        builder.Property(installment => installment.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(installment => installment.DueDate)
            .IsRequired();

        builder.Property(installment => installment.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(installment => new { installment.TenantId, installment.CreditCardStatementId });

        builder.HasIndex(installment => new { installment.TenantId, installment.CreditCardPurchaseId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(installment => installment.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CreditCardPurchase>()
            .WithMany()
            .HasForeignKey(installment => installment.CreditCardPurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CreditCardStatement>()
            .WithMany()
            .HasForeignKey(installment => installment.CreditCardStatementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
