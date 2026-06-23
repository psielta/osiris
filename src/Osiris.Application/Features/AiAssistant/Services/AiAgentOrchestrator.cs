using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Services;

/// <summary>
/// Drives one agent turn through an explicit, bounded tool loop. The model is asked for a reply; if it
/// requests tools, each call is validated against the registry and the execution policy, executed
/// server-side, redacted and fed back; this repeats until the model produces a final answer or a limit
/// is reached. No financial command is ever executed here — read tools only — and the loop always
/// terminates with a safe fallback.
/// </summary>
public sealed class AiAgentOrchestrator : IAiAgentOrchestrator
{
    private const string UnknownToolJson = "{\"error\":\"unknown_tool\"}";
    private const string LimitReachedJson = "{\"error\":\"tool_call_limit_reached\"}";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiModelClient _modelClient;
    private readonly IAiToolRegistry _toolRegistry;
    private readonly IAiToolExecutionPolicy _policy;
    private readonly IAiPromptBuilder _promptBuilder;
    private readonly IAiDataRedactor _redactor;
    private readonly AiAgentOptions _options;
    private readonly ILogger<AiAgentOrchestrator> _logger;

    public AiAgentOrchestrator(
        IAiModelClient modelClient,
        IAiToolRegistry toolRegistry,
        IAiToolExecutionPolicy policy,
        IAiPromptBuilder promptBuilder,
        IAiDataRedactor redactor,
        IOptions<AiAgentOptions> options,
        ILogger<AiAgentOrchestrator> logger)
    {
        _modelClient = modelClient;
        _toolRegistry = toolRegistry;
        _policy = policy;
        _promptBuilder = promptBuilder;
        _redactor = redactor;
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
                    toolResults.Add(new AiModelToolResult(call.Name, LimitReachedJson));
                    executed.Add(RejectedRecord(call.Name, AiToolRisk.Forbidden, call.Arguments, "limit_reached"));
                    continue;
                }

                var (modelResult, record, callSources, callProposals) = await ExecuteSingleAsync(context, call, cancellationToken);
                toolResults.Add(modelResult);
                executed.Add(record);
                sources.AddRange(callSources);
                proposals.AddRange(callProposals);
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

    private async Task<(AiModelToolResult ModelResult, AiToolCallRecord Record, IReadOnlyList<AiSource> Sources, IReadOnlyList<AiProposalReference> Proposals)>
        ExecuteSingleAsync(AiAgentContext context, AiModelToolCall call, CancellationToken cancellationToken)
    {
        var tool = _toolRegistry.Find(call.Name);
        if (tool is null)
        {
            _logger.LogWarning("AI model requested unknown tool {Tool}.", call.Name);
            return (
                new AiModelToolResult(call.Name, UnknownToolJson),
                RejectedRecord(call.Name, AiToolRisk.Forbidden, call.Arguments, "unknown_tool"),
                Array.Empty<AiSource>(),
                Array.Empty<AiProposalReference>());
        }

        var decision = _policy.Evaluate(context, tool);
        if (!decision.IsAllowed)
        {
            _logger.LogWarning("AI tool {Tool} denied by policy: {Reason}", tool.Name, decision.Reason);
            var deniedJson = JsonSerializer.Serialize(new { error = true, reason = decision.Reason }, SerializerOptions);
            return (
                new AiModelToolResult(tool.Name, deniedJson),
                RejectedRecord(tool.Name, tool.Risk, call.Arguments, "policy_denied"),
                Array.Empty<AiSource>(),
                Array.Empty<AiProposalReference>());
        }

        var sw = Stopwatch.StartNew();
        AiToolResult toolResult;
        try
        {
            toolResult = await tool.ExecuteAsync(call.Arguments, context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "AI tool {Tool} threw during execution.", tool.Name);
            toolResult = AiToolResult.Failure("Erro ao executar a ferramenta.");
        }

        sw.Stop();

        var status = toolResult.IsSuccess ? AiToolCallStatus.Succeeded : AiToolCallStatus.Failed;
        var record = new AiToolCallRecord(
            tool.Name,
            tool.Risk,
            status,
            _redactor.Redact(RawArguments(call.Arguments)),
            _redactor.Redact(toolResult.ResultJson),
            (int)sw.ElapsedMilliseconds,
            toolResult.IsSuccess ? null : "tool_error");

        return (
            new AiModelToolResult(tool.Name, toolResult.ResultJson),
            record,
            toolResult.Sources ?? Array.Empty<AiSource>(),
            toolResult.Proposals ?? Array.Empty<AiProposalReference>());
    }

    private AiToolCallRecord RejectedRecord(string toolName, AiToolRisk risk, JsonElement arguments, string errorCode) =>
        new(toolName, risk, AiToolCallStatus.Rejected, _redactor.Redact(RawArguments(arguments)), "{}", 0, errorCode);

    private static string RawArguments(JsonElement arguments) =>
        arguments.ValueKind == JsonValueKind.Undefined ? "{}" : arguments.GetRawText();

    private static AiUsage Accumulate(AiUsage current, AiUsage delta) =>
        new(
            current.InputTokens + delta.InputTokens,
            current.OutputTokens + delta.OutputTokens,
            current.CachedTokens + delta.CachedTokens);

    private static IReadOnlyList<AiSource> DistinctSources(IEnumerable<AiSource> sources) =>
        sources.DistinctBy(source => (source.Type, source.Id)).ToList();
}
