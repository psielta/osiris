using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Osiris.Application.Common.AI;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Services;

/// <summary>
/// Shared single-tool-call executor (extracted from the orchestrator so the realtime voice path reuses
/// the identical validate → policy → execute → redact → audit pipeline). Tenant scope comes from
/// <see cref="AiAgentContext"/>; nothing here trusts the model for tenant/user.
/// </summary>
public sealed class AiToolCallExecutor : IAiToolCallExecutor
{
    private const string UnknownToolJson = "{\"error\":\"unknown_tool\"}";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiToolRegistry _toolRegistry;
    private readonly IAiToolExecutionPolicy _policy;
    private readonly IAiDataRedactor _redactor;
    private readonly ILogger<AiToolCallExecutor> _logger;

    public AiToolCallExecutor(
        IAiToolRegistry toolRegistry,
        IAiToolExecutionPolicy policy,
        IAiDataRedactor redactor,
        ILogger<AiToolCallExecutor> logger)
    {
        _toolRegistry = toolRegistry;
        _policy = policy;
        _redactor = redactor;
        _logger = logger;
    }

    public async Task<AiToolCallOutcome> ExecuteAsync(
        AiAgentContext context,
        AiModelToolCall call,
        CancellationToken cancellationToken)
    {
        var tool = _toolRegistry.Find(call.Name);
        if (tool is null)
        {
            _logger.LogWarning("AI model requested unknown tool {Tool}.", call.Name);
            return Rejected(call, UnknownToolJson, AiToolRisk.Forbidden, "unknown_tool");
        }

        var decision = _policy.Evaluate(context, tool);
        if (!decision.IsAllowed)
        {
            _logger.LogWarning("AI tool {Tool} denied by policy: {Reason}", tool.Name, decision.Reason);
            var deniedJson = JsonSerializer.Serialize(new { error = true, reason = decision.Reason }, SerializerOptions);
            return Rejected(call, deniedJson, tool.Risk, "policy_denied");
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

        return new AiToolCallOutcome(
            new AiModelToolResult(tool.Name, toolResult.ResultJson),
            record,
            toolResult.Sources ?? Array.Empty<AiSource>(),
            toolResult.Proposals ?? Array.Empty<AiProposalReference>());
    }

    public AiToolCallOutcome Reject(AiModelToolCall call, string resultJson, string errorCode) =>
        Rejected(call, resultJson, AiToolRisk.Forbidden, errorCode);

    private AiToolCallOutcome Rejected(AiModelToolCall call, string resultJson, AiToolRisk risk, string errorCode) =>
        new(
            new AiModelToolResult(call.Name, resultJson),
            new AiToolCallRecord(
                call.Name, risk, AiToolCallStatus.Rejected,
                _redactor.Redact(RawArguments(call.Arguments)), "{}", 0, errorCode),
            Array.Empty<AiSource>(),
            Array.Empty<AiProposalReference>());

    private static string RawArguments(JsonElement arguments) =>
        arguments.ValueKind == JsonValueKind.Undefined ? "{}" : arguments.GetRawText();
}
