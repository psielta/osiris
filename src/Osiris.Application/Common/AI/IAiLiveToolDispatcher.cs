namespace Osiris.Application.Common.AI;

/// <summary>
/// Executes a batch of tool calls emitted by a realtime (voice) session, reusing the same per-call
/// executor as the text turn. Returns model-facing results to feed back over the live socket, plus the
/// audit records, sources and write proposals to surface to the client.
/// </summary>
public interface IAiLiveToolDispatcher
{
    Task<AiLiveToolBatch> DispatchAsync(
        AiAgentContext context,
        IReadOnlyList<AiModelToolCall> calls,
        CancellationToken cancellationToken);
}

public sealed record AiLiveToolBatch(
    IReadOnlyList<AiModelToolResult> Results,
    IReadOnlyList<AiToolCallRecord> Records,
    IReadOnlyList<AiSource> Sources,
    IReadOnlyList<AiProposalReference> Proposals);
