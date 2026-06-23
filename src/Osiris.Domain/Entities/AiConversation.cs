using Osiris.Domain.Common;
using Osiris.Domain.Enums;

namespace Osiris.Domain.Entities;

/// <summary>
/// A single AI assistant conversation, private to one user within one tenant. The conversation is
/// the audit anchor for every message, tool call and proposal produced during the dialogue.
/// </summary>
public sealed class AiConversation : BaseEntity
{
    private AiConversation() { }

    public AiConversation(Guid tenantId, string userId, string title, string promptVersion)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        TenantId = tenantId;
        UserId = userId;
        Title = NormalizeTitle(title);
        PromptVersion = string.IsNullOrWhiteSpace(promptVersion) ? "unknown" : promptVersion.Trim();
        Status = AiConversationStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public AiConversationStatus Status { get; private set; }
    public string PromptVersion { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public DateTime? SummaryUpdatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? ArchivedAtUtc { get; private set; }

    public bool IsActive => Status == AiConversationStatus.Active;

    public void Touch(DateTime utcNow) => UpdatedAtUtc = utcNow;

    public void Rename(string title, DateTime utcNow)
    {
        Title = NormalizeTitle(title);
        UpdatedAtUtc = utcNow;
    }

    public void UpdateSummary(string? summary, DateTime utcNow)
    {
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        SummaryUpdatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Archive(DateTime utcNow)
    {
        if (Status == AiConversationStatus.Archived)
        {
            return;
        }

        Status = AiConversationStatus.Archived;
        ArchivedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Nova conversa";
        }

        var trimmed = title.Trim();
        return trimmed.Length > 120 ? trimmed[..120] : trimmed;
    }
}
