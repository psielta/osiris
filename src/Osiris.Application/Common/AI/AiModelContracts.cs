using System.Text.Json;

namespace Osiris.Application.Common.AI;

/// <summary>
/// Provider-neutral message/tool protocol shared between the orchestrator and any model client.
/// No Google/Gemini SDK type ever crosses into Application — the Infrastructure adapter maps to and
/// from these types. This keeps the Application layer ignorant of the concrete AI provider.
/// </summary>
public enum AiModelRole
{
    /// <summary>The end user.</summary>
    User = 1,

    /// <summary>The assistant model (text reply or tool-call request).</summary>
    Model = 2,

    /// <summary>Tool execution results fed back to the model.</summary>
    Tool = 3
}

/// <summary>Logical model selection; the adapter resolves this to a concrete configured model name.</summary>
public enum AiModelPurpose
{
    Agent = 1,
    Fast = 2
}

public enum AiFinishReason
{
    Stop = 1,
    ToolCalls = 2,
    MaxTokens = 3,
    Safety = 4,
    Other = 5
}

public sealed record AiUsage(int InputTokens, int OutputTokens, int CachedTokens)
{
    public static AiUsage Empty { get; } = new(0, 0, 0);
}

/// <summary>
/// A single tool-call request emitted by the model. <see cref="Id"/> may be empty for providers (such
/// as Gemini) that match responses by name/order. <see cref="Signature"/> is an opaque provider token
/// (Gemini's <c>thoughtSignature</c>) that must be echoed back with the call on the next turn.
/// </summary>
public sealed record AiModelToolCall(string Id, string Name, JsonElement Arguments, string? Signature = null);

/// <summary>
/// A tool result returned to the model, already serialized by the server. <see cref="Id"/> correlates to
/// the originating call's id (used by the Live API <c>functionResponse</c>); null in the text turn loop.
/// </summary>
public sealed record AiModelToolResult(string Name, string ResultJson, string? Id = null);

/// <summary>The JSON-schema description of a tool the model is allowed to call this turn.</summary>
public sealed record AiToolDefinition(string Name, string Description, object ParametersSchema);

/// <summary>One neutral conversation message (user text, model text/tool-calls, or tool results).</summary>
public sealed record AiModelMessage
{
    public required AiModelRole Role { get; init; }
    public string? Text { get; init; }
    public IReadOnlyList<AiModelToolCall> ToolCalls { get; init; } = Array.Empty<AiModelToolCall>();
    public IReadOnlyList<AiModelToolResult> ToolResults { get; init; } = Array.Empty<AiModelToolResult>();

    public static AiModelMessage FromUser(string text) =>
        new() { Role = AiModelRole.User, Text = text };

    public static AiModelMessage FromAssistant(string text) =>
        new() { Role = AiModelRole.Model, Text = text };

    public static AiModelMessage FromModelToolCalls(IReadOnlyList<AiModelToolCall> toolCalls) =>
        new() { Role = AiModelRole.Model, ToolCalls = toolCalls };

    public static AiModelMessage FromToolResults(IReadOnlyList<AiModelToolResult> toolResults) =>
        new() { Role = AiModelRole.Tool, ToolResults = toolResults };
}

/// <summary>A single request to the model: system prompt, the running conversation, and allowed tools.</summary>
public sealed record AiModelRequest(
    AiModelPurpose Purpose,
    string SystemPrompt,
    IReadOnlyList<AiModelMessage> Messages,
    IReadOnlyList<AiToolDefinition> Tools,
    string CorrelationId);

/// <summary>The model's reply for one request: text and/or tool calls, with usage and finish reason.</summary>
public sealed record AiModelTurnResult(
    string? Text,
    IReadOnlyList<AiModelToolCall> ToolCalls,
    AiUsage Usage,
    AiFinishReason FinishReason,
    string ModelName)
{
    public bool HasToolCalls => ToolCalls.Count > 0;
}
