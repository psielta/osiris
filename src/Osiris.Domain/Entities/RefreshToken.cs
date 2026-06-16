using Osiris.Domain.Common;

namespace Osiris.Domain.Entities;

/// <summary>
/// A long-lived refresh token for API/mobile sessions. Only the SHA-256 hash of the raw token is
/// persisted, so a database leak cannot be replayed. Rotation is tracked through
/// <see cref="ReplacedByTokenId"/>; reuse of a revoked token signals theft.
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    private RefreshToken()
    {
    }

    public RefreshToken(string tokenHash, string userId, Guid tenantId, DateTime expiresAtUtc)
    {
        TokenHash = tokenHash;
        UserId = userId;
        TenantId = tenantId;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string TokenHash { get; private set; } = string.Empty;

    public string UserId { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && utcNow < ExpiresAtUtc;

    public void Revoke(DateTime utcNow, Guid? replacedByTokenId = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = utcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
