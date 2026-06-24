using Osiris.Domain.Common;
using Osiris.Domain.Enums;

namespace Osiris.Domain.Entities;

/// <summary>
/// One persisted turn message. Assistant messages carry usage/cost metadata and the prompt version
/// (plus a hash) that produced them; only the user-visible content is stored, never hidden reasoning.
/// </summary>
public sealed class AiMessage : BaseEntity
{
    private AiMessage() { }

    private AiMessage(
        Guid tenantId,
        Guid conversationId,
        string userId,
        AiMessageRole role,
        string content,
        string channel)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("Conversation id is required.", nameof(conversationId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        TenantId = tenantId;
        ConversationId = conversationId;
        UserId = userId;
        Role = role;
        Content = content ?? string.Empty;
        Channel = NormalizeChannel(channel);
    }

    public Guid TenantId { get; private set; }
    public Guid ConversationId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public AiMessageRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string Channel { get; private set; } = "text";
    public string? Model { get; private set; }
    public string? PromptVersion { get; private set; }
    public string? PromptHash { get; private set; }
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int CachedTokens { get; private set; }
    public int InputAudioMilliseconds { get; private set; }
    public int OutputAudioMilliseconds { get; private set; }
    public int LatencyMs { get; private set; }
    public string? FinishReason { get; private set; }
    public string? CorrelationId { get; private set; }

    public static AiMessage ForUser(Guid tenantId, Guid conversationId, string userId, string content)
        => new(tenantId, conversationId, userId, AiMessageRole.User, content, "text");

    public static AiMessage ForVoiceUser(
        Guid tenantId,
        Guid conversationId,
        string userId,
        string content,
        int inputAudioMilliseconds)
    {
        return new AiMessage(tenantId, conversationId, userId, AiMessageRole.User, content, "voice")
        {
            InputAudioMilliseconds = Math.Max(0, inputAudioMilliseconds)
        };
    }

    public static AiMessage ForTool(Guid tenantId, Guid conversationId, string userId, string content)
        => new(tenantId, conversationId, userId, AiMessageRole.Tool, content, "text");

    public static AiMessage ForAssistant(
        Guid tenantId,
        Guid conversationId,
        string userId,
        string content,
        string? model,
        string? promptVersion,
        string? promptHash,
        int inputTokens,
        int outputTokens,
        int cachedTokens,
        int latencyMs,
        string? finishReason,
        string? correlationId,
        string channel = "text",
        int inputAudioMilliseconds = 0,
        int outputAudioMilliseconds = 0)
    {
        return new AiMessage(tenantId, conversationId, userId, AiMessageRole.Assistant, content, channel)
        {
            Model = model,
            PromptVersion = promptVersion,
            PromptHash = promptHash,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CachedTokens = cachedTokens,
            InputAudioMilliseconds = Math.Max(0, inputAudioMilliseconds),
            OutputAudioMilliseconds = Math.Max(0, outputAudioMilliseconds),
            LatencyMs = latencyMs,
            FinishReason = finishReason,
            CorrelationId = correlationId
        };
    }

    private static string NormalizeChannel(string? channel)
    {
        var normalized = string.IsNullOrWhiteSpace(channel) ? "text" : channel.Trim().ToLowerInvariant();
        return normalized.Length > 20 ? normalized[..20] : normalized;
    }
}
