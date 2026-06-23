using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;

namespace Osiris.Application.Features.AiAssistant.Services;

/// <summary>
/// Drives one agent turn through an explicit, bounded tool loop. The model is asked for a reply; if it
/// requests tools, each call is executed via the shared <see cref="IAiToolCallExecutor"/> (validate →
/// policy → execute → redact → audit) and fed back; this repeats until the model produces a final answer
/// or a limit is reached. No financial command is ever executed here — read tools only — and the loop
/// always terminates with a safe fallback.
/// </summary>
public sealed class AiAgentOrchestrator : IAiAgentOrchestrator
{
    private const string LimitReachedJson = "{\"error\":\"tool_call_limit_reached\"}";

    private readonly IAiModelClient _modelClient;
    private readonly IAiToolRegistry _toolRegistry;
    private readonly IAiToolCallExecutor _executor;
    private readonly IAiPromptBuilder _promptBuilder;
    private readonly AiAgentOptions _options;
    private readonly ILogger<AiAgentOrchestrator> _logger;

    public AiAgentOrchestrator(
        IAiModelClient modelClient,
        IAiToolRegistry toolRegistry,
        IAiToolCallExecutor executor,
        IAiPromptBuilder promptBuilder,
        IOptions<AiAgentOptions> options,
        ILogger<AiAgentOrchestrator> logger)
    {
        _modelClient = modelClient;
        _toolRegistry = toolRegistry;
        _executor = executor;
        _promptBuilder = promptBuilder;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiTurnResult> RunAsync(
        AiAgentContext context,
        IReadOnlyList<AiModelMessage> priorMessages,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var prompt = _promptBuilder.BuildSystemPrompt(context);
        var toolDefinitions = _toolRegistry.GetAllowedTools(context)
            .Select(tool => new AiToolDefinition(tool.Name, tool.Description, tool.InputSchema))
            .ToList();

        var messages = new List<AiModelMessage>(priorMessages) { AiModelMessage.FromUser(userMessage) };
        var executed = new List<AiToolCallRecord>();
        var sources = new List<AiSource>();
        var proposals = new List<AiProposalReference>();
        var usage = AiUsage.Empty;
        var finishReason = AiFinishReason.Other;
        var modelName = string.Empty;
        string? finalText = null;
        var totalToolCalls = 0;

        for (var iteration = 0; iteration < _options.MaxToolIterations; iteration++)
        {
            var request = new AiModelRequest(
                AiModelPurpose.Agent,
                prompt.SystemPrompt,
                messages,
                toolDefinitions,
                context.CorrelationId);

            var result = await _modelClient.GenerateAsync(request, cancellationToken);

            usage = Accumulate(usage, result.Usage);
            finishReason = result.FinishReason;
            if (!string.IsNullOrEmpty(result.ModelName))
            {
                modelName = result.ModelName;
            }

            if (!result.HasToolCalls)
            {
                finalText = result.Text ?? string.Empty;
                break;
            }

            messages.Add(AiModelMessage.FromModelToolCalls(result.ToolCalls));

            var toolResults = new List<AiModelToolResult>(result.ToolCalls.Count);
            foreach (var call in result.ToolCalls)
            {
                totalToolCalls++;
                if (totalToolCalls > _options.MaxToolCallsPerTurn)
                {
                    _logger.LogWarning(
                        "AI turn {CorrelationId} reached the per-turn tool-call limit ({Limit}).",
                        context.CorrelationId,
                        _options.MaxToolCallsPerTurn);
                    var limited = _executor.Reject(call, LimitReachedJson, "limit_reached");
                    toolResults.Add(limited.ModelResult);
                    executed.Add(limited.Record);
                    continue;
                }

                var outcome = await _executor.ExecuteAsync(context, call, cancellationToken);
                toolResults.Add(outcome.ModelResult);
                executed.Add(outcome.Record);
                sources.AddRange(outcome.Sources);
                proposals.AddRange(outcome.Proposals);
            }

            messages.Add(AiModelMessage.FromToolResults(toolResults));
        }

        stopwatch.Stop();

        if (finalText is null)
        {
            _logger.LogWarning(
                "AI turn {CorrelationId} exhausted the iteration budget without a final answer.",
                context.CorrelationId);
            finalText = "Não consegui concluir a resposta agora. Pode reformular a pergunta ou tentar novamente?";
            finishReason = AiFinishReason.Other;
        }

        return new AiTurnResult(
            finalText,
            executed,
            DistinctSources(sources),
            proposals,
            usage,
            finishReason,
            string.IsNullOrEmpty(modelName) ? "unknown" : modelName,
            prompt.Version,
            prompt.Hash,
            (int)stopwatch.ElapsedMilliseconds);
    }

    private static AiUsage Accumulate(AiUsage current, AiUsage delta) =>
        new(
            current.InputTokens + delta.InputTokens,
            current.OutputTokens + delta.OutputTokens,
            current.CachedTokens + delta.CachedTokens);

    private static IReadOnlyList<AiSource> DistinctSources(IEnumerable<AiSource> sources) =>
        sources.DistinctBy(source => (source.Type, source.Id)).ToList();
}
