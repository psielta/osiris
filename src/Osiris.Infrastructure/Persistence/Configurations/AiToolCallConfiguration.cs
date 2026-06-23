using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class AiToolCallConfiguration : IEntityTypeConfiguration<AiToolCall>
{
    public void Configure(EntityTypeBuilder<AiToolCall> builder)
    {
        builder.ToTable("AiToolCalls");

        builder.HasKey(toolCall => toolCall.Id);

        builder.Property(toolCall => toolCall.TenantId)
            .IsRequired();

        builder.Property(toolCall => toolCall.ConversationId)
            .IsRequired();

        builder.Property(toolCall => toolCall.MessageId)
            .IsRequired();

        builder.Property(toolCall => toolCall.ToolName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(toolCall => toolCall.Risk)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(toolCall => toolCall.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(toolCall => toolCall.ArgumentsJsonRedacted)
            .IsRequired();

        builder.Property(toolCall => toolCall.ResultJsonRedacted)
            .IsRequired();

        builder.Property(toolCall => toolCall.DurationMs)
            .IsRequired();

        builder.Property(toolCall => toolCall.ErrorCode)
            .HasMaxLength(100);

        builder.Property(toolCall => toolCall.CompletedAtUtc)
            .IsRequired();

        builder.Property(toolCall => toolCall.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(toolCall => new { toolCall.TenantId, toolCall.ConversationId });

        builder.HasIndex(toolCall => toolCall.MessageId);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(toolCall => toolCall.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AiConversation>()
            .WithMany()
            .HasForeignKey(toolCall => toolCall.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
