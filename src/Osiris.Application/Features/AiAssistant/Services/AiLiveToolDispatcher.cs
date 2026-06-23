using Osiris.Application.Common.AI;

namespace Osiris.Application.Features.AiAssistant.Services;

/// <summary>
/// Runs every tool call from a live turn through the shared <see cref="IAiToolCallExecutor"/> and
/// aggregates the outcomes. Sequential by default (deterministic auditing and ordering); the Live API
/// can still keep streaming audio because read tools are declared <c>NON_BLOCKING</c> at the wire level.
/// </summary>
public sealed class AiLiveToolDispatcher : IAiLiveToolDispatcher
{
    private readonly IAiToolCallExecutor _executor;

    public AiLiveToolDispatcher(IAiToolCallExecutor executor)
    {
        _executor = executor;
    }

    public async Task<AiLiveToolBatch> DispatchAsync(
        AiAgentContext context,
        IReadOnlyList<AiModelToolCall> calls,
        CancellationToken cancellationToken)
    {
        var results = new List<AiModelToolResult>(calls.Count);
        var records = new List<AiToolCallRecord>(calls.Count);
        var sources = new List<AiSource>();
        var proposals = new List<AiProposalReference>();

        foreach (var call in calls)
        {
            var outcome = await _executor.ExecuteAsync(context, call, cancellationToken);
            results.Add(outcome.ModelResult);
            records.Add(outcome.Record);
            sources.AddRange(outcome.Sources);
            proposals.AddRange(outcome.Proposals);
        }

        return new AiLiveToolBatch(
            results,
            records,
            sources.DistinctBy(source => (source.Type, source.Id)).ToList(),
            proposals);
    }
}
