using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Domain.Enums;

namespace Osiris.Ai.EvaluationTests.Support;

/// <summary>Scripts the model to request a given tool on the first turn, then answer once a tool result exists.</summary>
internal sealed class ScriptedAiModelClient : IAiModelClient
{
    private readonly string _toolToCall;

    public ScriptedAiModelClient(string toolToCall)
    {
        _toolToCall = toolToCall;
    }

    public Task<AiModelTurnResult> GenerateAsync(AiModelRequest request, CancellationToken cancellationToken)
    {
        var hasToolResult = request.Messages.Any(message => message.Role == AiModelRole.Tool);
        if (!hasToolResult)
        {
            var call = new AiModelToolCall(string.Empty, _toolToCall, JsonDocument.Parse("{}").RootElement.Clone());
            return Task.FromResult(new AiModelTurnResult(
                null,
                new[] { call },
                new AiUsage(5, 0, 0),
                AiFinishReason.ToolCalls,
                "scripted"));
        }

        return Task.FromResult(new AiModelTurnResult(
            "Pronto.",
            Array.Empty<AiModelToolCall>(),
            new AiUsage(3, 3, 0),
            AiFinishReason.Stop,
            "scripted"));
    }
}

/// <summary>A tool that records whether it ran, with a configurable risk and optional sources.</summary>
internal sealed class RecordingTool : IAiTool
{
    private readonly bool _withSources;

    public RecordingTool(string name, AiToolRisk risk, bool withSources = false)
    {
        Name = name;
        Risk = risk;
        _withSources = withSources;
    }

    public int ExecutionCount { get; private set; }

    public string Name { get; }

    public string Description => $"recording tool {Name}";

    public AiToolRisk Risk { get; }

    public object InputSchema => new { type = "object", properties = new { } };

    public Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        IReadOnlyList<AiSource>? sources = _withSources ? new[] { new AiSource("entity", "1", "Exemplo") } : null;
        return Task.FromResult(AiToolResult.Success("{\"ok\":true}", sources));
    }
}

/// <summary>Pass-through redactor for evaluation runs.</summary>
internal sealed class NoOpRedactor : IAiDataRedactor
{
    public string Redact(string? text) => text ?? string.Empty;
}

/// <summary>An <see cref="ISender"/> that must never be invoked; used only to construct real tools for schema/risk inspection.</summary>
internal sealed class ThrowingSender : ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The evaluation must not execute real queries.");

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest =>
        throw new NotSupportedException("The evaluation must not execute real queries.");

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The evaluation must not execute real queries.");

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
