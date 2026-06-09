using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class CreditCardStatementConfiguration : IEntityTypeConfiguration<CreditCardStatement>
{
    public void Configure(EntityTypeBuilder<CreditCardStatement> builder)
    {
        builder.ToTable("CreditCardStatements");

        builder.HasKey(statement => statement.Id);

        builder.Property(statement => statement.TenantId)
            .IsRequired();

        builder.Property(statement => statement.CreditCardId)
            .IsRequired();

        builder.Property(statement => statement.ReferenceMonth)
            .IsRequired();

        builder.Property(statement => statement.ReferenceYear)
            .IsRequired();

        builder.Property(statement => statement.ClosingDate)
            .IsRequired();

        builder.Property(statement => statement.DueDate)
            .IsRequired();

        builder.Property(statement => statement.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(statement => statement.CreatedAtUtc)
            .IsRequired();

        builder.Property(statement => statement.UpdatedAtUtc);

        builder.HasIndex(statement => statement.TenantId);

        builder.HasIndex(statement => new
        {
            statement.TenantId,
            statement.CreditCardId,
            statement.ReferenceYear,
            statement.ReferenceMonth
        }).IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(statement => statement.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CreditCard>()
            .WithMany()
            .HasForeignKey(statement => statement.CreditCardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
