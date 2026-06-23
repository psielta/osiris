namespace Osiris.Application.Common.AI;

/// <summary>
/// Executes a single model-requested tool call against the registry and the execution policy, with
/// redaction and audit. This is the shared unit reused by the text turn loop (<c>AiAgentOrchestrator</c>)
/// and the realtime voice path (<c>IAiLiveToolDispatcher</c>) — both validate, gate and execute tools the
/// exact same way, so security and auditing never diverge between channels.
/// </summary>
public interface IAiToolCallExecutor
{
    Task<AiToolCallOutcome> ExecuteAsync(AiAgentContext context, AiModelToolCall call, CancellationToken cancellationToken);

    /// <summary>Builds a rejected outcome (e.g. limit reached) without running any tool.</summary>
    AiToolCallOutcome Reject(AiModelToolCall call, string resultJson, string errorCode);
}

/// <summary>
/// The result of executing one tool call: the model-facing result, the audit record, and any sources or
/// write proposals the tool surfaced.
/// </summary>
public sealed record AiToolCallOutcome(
    AiModelToolResult ModelResult,
    AiToolCallRecord Record,
    IReadOnlyList<AiSource> Sources,
    IReadOnlyList<AiProposalReference> Proposals);
