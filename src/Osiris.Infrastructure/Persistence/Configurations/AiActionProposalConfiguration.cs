using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class AiActionProposalConfiguration : IEntityTypeConfiguration<AiActionProposal>
{
    public void Configure(EntityTypeBuilder<AiActionProposal> builder)
    {
        builder.ToTable("AiActionProposals");

        builder.HasKey(proposal => proposal.Id);

        builder.Property(proposal => proposal.TenantId)
            .IsRequired();

        builder.Property(proposal => proposal.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(proposal => proposal.ConversationId)
            .IsRequired();

        builder.Property(proposal => proposal.ActionType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(proposal => proposal.PayloadJson)
            .IsRequired();

        builder.Property(proposal => proposal.DisplaySummary)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(proposal => proposal.ImpactSummary)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(proposal => proposal.RiskLevel)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(proposal => proposal.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(proposal => proposal.IdempotencyKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(proposal => proposal.StateHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(proposal => proposal.ExpiresAtUtc)
            .IsRequired();

        builder.Property(proposal => proposal.ConfirmedAtUtc);

        builder.Property(proposal => proposal.ExecutedAtUtc);

        builder.Property(proposal => proposal.ResultEntityType)
            .HasMaxLength(100);

        builder.Property(proposal => proposal.ResultEntityId);

        builder.Property(proposal => proposal.FailureCode)
            .HasMaxLength(100);

        builder.Property(proposal => proposal.FailureMessage)
            .HasMaxLength(1000);

        builder.Property(proposal => proposal.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(proposal => new { proposal.TenantId, proposal.Status });

        builder.HasIndex(proposal => new { proposal.TenantId, proposal.IdempotencyKey })
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(proposal => proposal.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AiConversation>()
            .WithMany()
            .HasForeignKey(proposal => proposal.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
