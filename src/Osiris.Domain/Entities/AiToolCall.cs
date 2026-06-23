using Osiris.Domain.Common;
using Osiris.Domain.Enums;

namespace Osiris.Domain.Entities;

/// <summary>
/// Audit record of a single tool invocation during a turn. Arguments and result are stored already
/// redacted; this row exists so every data access the agent performed can be reviewed after the fact.
/// </summary>
public sealed class AiToolCall : BaseEntity
{
    private AiToolCall() { }

    public AiToolCall(
        Guid tenantId,
        Guid conversationId,
        Guid messageId,
        string toolName,
        AiToolRisk risk,
        AiToolCallStatus status,
        string argumentsJsonRedacted,
        string resultJsonRedacted,
        int durationMs,
        string? errorCode,
        DateTime completedAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("Conversation id is required.", nameof(conversationId));
        }

        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name is required.", nameof(toolName));
        }

        TenantId = tenantId;
        ConversationId = conversationId;
        MessageId = messageId;
        ToolName = toolName;
        Risk = risk;
        Status = status;
        ArgumentsJsonRedacted = argumentsJsonRedacted ?? string.Empty;
        ResultJsonRedacted = resultJsonRedacted ?? string.Empty;
        DurationMs = durationMs;
        ErrorCode = errorCode;
        CompletedAtUtc = completedAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid MessageId { get; private set; }
    public string ToolName { get; private set; } = string.Empty;
    public AiToolRisk Risk { get; private set; }
    public AiToolCallStatus Status { get; private set; }
    public string ArgumentsJsonRedacted { get; private set; } = string.Empty;
    public string ResultJsonRedacted { get; private set; } = string.Empty;
    public int DurationMs { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }
}
