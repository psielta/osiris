using Osiris.Domain.Enums;

namespace Osiris.Application.Common.AI;

/// <summary>
/// Audit-shaped record of one executed tool call, ready to be persisted. Arguments and result are
/// already redacted by the orchestrator.
/// </summary>
public sealed record AiToolCallRecord(
    string ToolName,
    AiToolRisk Risk,
    AiToolCallStatus Status,
    string ArgumentsJsonRedacted,
    string ResultJsonRedacted,
    int DurationMs,
    string? ErrorCode);

/// <summary>
/// The outcome of a full turn: the assistant's final text, every tool call executed (for audit), the
/// aggregated usage, and the prompt identity that produced it. The handler persists from this.
/// </summary>
public sealed record AiTurnResult(
    string AssistantText,
    IReadOnlyList<AiToolCallRecord> ExecutedToolCalls,
    IReadOnlyList<AiSource> Sources,
    IReadOnlyList<AiProposalReference> Proposals,
    AiUsage Usage,
    AiFinishReason FinishReason,
    string ModelName,
    string PromptVersion,
    string PromptHash,
    int LatencyMs);

/// <summary>
/// Runs one agent turn: builds the prompt, calls the model, validates and executes allowed tools in a
/// bounded loop, and returns a final answer. It performs no persistence — that is the caller's job —
/// and never executes a financial command during the model's turn.
/// </summary>
public interface IAiAgentOrchestrator
{
    Task<AiTurnResult> RunAsync(
        AiAgentContext context,
        IReadOnlyList<AiModelMessage> priorMessages,
        string userMessage,
        CancellationToken cancellationToken);
}
