using Osiris.Domain.Common;

namespace Osiris.Domain.Entities;

/// <summary>
/// User feedback about an assistant message (thumbs up/down plus an optional reason). Stored for
/// quality monitoring; not exposed by the foundation slice but present so it can be wired later.
/// </summary>
public sealed class AiFeedback : BaseEntity
{
    private AiFeedback() { }

    public AiFeedback(
        Guid tenantId,
        string userId,
        Guid messageId,
        int rating,
        string? reasonCode,
        string? comment)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        TenantId = tenantId;
        UserId = userId;
        MessageId = messageId;
        Rating = Math.Sign(rating);
        ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? null : reasonCode.Trim();
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }

    public Guid TenantId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid MessageId { get; private set; }

    /// <summary>Normalized to -1 (negative), 0 (neutral) or +1 (positive).</summary>
    public int Rating { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? Comment { get; private set; }
}
