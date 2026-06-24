using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        builder.ToTable("AiMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.TenantId)
            .IsRequired();

        builder.Property(message => message.ConversationId)
            .IsRequired();

        builder.Property(message => message.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(message => message.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(message => message.Content)
            .IsRequired();

        builder.Property(message => message.Channel)
            .HasMaxLength(20)
            .HasDefaultValue("text")
            .IsRequired();

        builder.Property(message => message.Model)
            .HasMaxLength(100);

        builder.Property(message => message.PromptVersion)
            .HasMaxLength(50);

        builder.Property(message => message.PromptHash)
            .HasMaxLength(64);

        builder.Property(message => message.InputTokens)
            .IsRequired();

        builder.Property(message => message.OutputTokens)
            .IsRequired();

        builder.Property(message => message.CachedTokens)
            .IsRequired();

        builder.Property(message => message.InputAudioMilliseconds)
            .IsRequired();

        builder.Property(message => message.OutputAudioMilliseconds)
            .IsRequired();

        builder.Property(message => message.LatencyMs)
            .IsRequired();

        builder.Property(message => message.FinishReason)
            .HasMaxLength(50);

        builder.Property(message => message.CorrelationId)
            .HasMaxLength(64);

        builder.Property(message => message.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(message => new { message.TenantId, message.ConversationId, message.CreatedAtUtc });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(message => message.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AiConversation>()
            .WithMany()
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
