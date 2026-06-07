using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(tenant => tenant.CreatedAtUtc)
            .IsRequired();
    }
}
