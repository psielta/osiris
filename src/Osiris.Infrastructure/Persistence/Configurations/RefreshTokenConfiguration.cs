using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osiris.Domain.Entities;
using Osiris.Infrastructure.Identity;

namespace Osiris.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        // Matches the AspNetUsers string key length.
        builder.Property(token => token.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(token => token.TenantId)
            .IsRequired();

        builder.Property(token => token.ExpiresAtUtc)
            .IsRequired();

        builder.Property(token => token.CreatedAtUtc)
            .IsRequired();

        builder.Property(token => token.RevokedAtUtc);

        builder.Property(token => token.ReplacedByTokenId);

        // Optimistic concurrency token. Npgsql maps a uint row-version property to the PostgreSQL
        // system column xmin, so two simultaneous rotations of the same token conflict and only one wins.
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.HasIndex(token => token.TenantId);

        builder.HasIndex(token => new { token.UserId, token.RevokedAtUtc });

        // Tokens are owned session state: deleting a user cleans up their tokens.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
