using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class CreditCardStatementPaymentConfiguration : IEntityTypeConfiguration<CreditCardStatementPayment>
{
    public void Configure(EntityTypeBuilder<CreditCardStatementPayment> builder)
    {
        builder.ToTable("CreditCardStatementPayments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.TenantId)
            .IsRequired();

        builder.Property(payment => payment.CreditCardStatementId)
            .IsRequired();

        builder.Property(payment => payment.FinancialAccountId);

        builder.Property(payment => payment.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payment => payment.PaidAt)
            .IsRequired();

        builder.Property(payment => payment.Notes)
            .HasMaxLength(500);

        builder.Property(payment => payment.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(payment => payment.TenantId);

        builder.HasIndex(payment => new { payment.TenantId, payment.CreditCardStatementId });

        builder.HasIndex(payment => new { payment.TenantId, payment.PaidAt });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(payment => payment.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CreditCardStatement>()
            .WithMany()
            .HasForeignKey(payment => payment.CreditCardStatementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinancialAccount>()
            .WithMany()
            .HasForeignKey(payment => payment.FinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
