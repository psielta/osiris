using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class AiFeedbackConfiguration : IEntityTypeConfiguration<AiFeedback>
{
    public void Configure(EntityTypeBuilder<AiFeedback> builder)
    {
        builder.ToTable("AiFeedbacks");

        builder.HasKey(feedback => feedback.Id);

        builder.Property(feedback => feedback.TenantId)
            .IsRequired();

        builder.Property(feedback => feedback.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(feedback => feedback.MessageId)
            .IsRequired();

        builder.Property(feedback => feedback.Rating)
            .IsRequired();

        builder.Property(feedback => feedback.ReasonCode)
            .HasMaxLength(100);

        builder.Property(feedback => feedback.Comment)
            .HasMaxLength(2000);

        builder.Property(feedback => feedback.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(feedback => new { feedback.TenantId, feedback.MessageId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(feedback => feedback.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
