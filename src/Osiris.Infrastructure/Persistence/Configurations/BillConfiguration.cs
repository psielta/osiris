using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.ToTable("Bills");

        builder.HasKey(bill => bill.Id);

        builder.Property(bill => bill.TenantId)
            .IsRequired();

        builder.Property(bill => bill.CategoryId)
            .IsRequired();

        builder.Property(bill => bill.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(bill => bill.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(bill => bill.DueDate)
            .IsRequired();

        builder.Property(bill => bill.PaidAt);

        builder.Property(bill => bill.PaymentAccountId);

        builder.Property(bill => bill.Notes)
            .HasMaxLength(500);

        builder.Property(bill => bill.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(bill => new { bill.TenantId, bill.DueDate });

        builder.HasIndex(bill => new { bill.TenantId, bill.CategoryId });

        builder.HasIndex(bill => new { bill.TenantId, bill.PaymentAccountId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(bill => bill.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinancialCategory>()
            .WithMany()
            .HasForeignKey(bill => bill.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinancialAccount>()
            .WithMany()
            .HasForeignKey(bill => bill.PaymentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
