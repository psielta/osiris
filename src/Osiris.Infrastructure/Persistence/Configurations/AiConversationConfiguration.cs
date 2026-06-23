using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("AiConversations");

        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation => conversation.TenantId)
            .IsRequired();

        builder.Property(conversation => conversation.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(conversation => conversation.Title)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(conversation => conversation.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(conversation => conversation.PromptVersion)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(conversation => conversation.Summary)
            .HasMaxLength(4000);

        builder.Property(conversation => conversation.SummaryUpdatedAtUtc);

        builder.Property(conversation => conversation.UpdatedAtUtc);

        builder.Property(conversation => conversation.ArchivedAtUtc);

        builder.Property(conversation => conversation.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(conversation => new { conversation.TenantId, conversation.UserId, conversation.UpdatedAtUtc })
            .IsDescending(false, false, true);

        builder.HasIndex(conversation => new { conversation.TenantId, conversation.Id });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(conversation => conversation.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
