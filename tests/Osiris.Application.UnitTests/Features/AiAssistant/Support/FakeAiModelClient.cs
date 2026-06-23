using System.Text.Json;
using Osiris.Application.Common.AI;

namespace Osiris.Application.UnitTests.Features.AiAssistant.Support;

/// <summary>
/// Scriptable <see cref="IAiModelClient"/>: returns the queued turns in order. With
/// <see cref="RepeatLast"/> it keeps returning the final queued turn, which is used to exercise the
/// orchestrator's loop/iteration limits.
/// </summary>
internal sealed class FakeAiModelClient : IAiModelClient
{
    private readonly Queue<AiModelTurnResult> _responses;
    private AiModelTurnResult? _last;

    public FakeAiModelClient(params AiModelTurnResult[] responses)
    {
        _responses = new Queue<AiModelTurnResult>(responses);
    }

    public bool RepeatLast { get; init; }

    public List<AiModelRequest> Requests { get; } = new();

    public Task<AiModelTurnResult> GenerateAsync(AiModelRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (_responses.Count > 0)
        {
            _last = _responses.Dequeue();
            return Task.FromResult(_last);
        }

        if (RepeatLast && _last is not null)
        {
            return Task.FromResult(_last);
        }

        return Task.FromResult(Text("(sem resposta)"));
    }

    public static AiModelTurnResult Text(string text) =>
        new(text, Array.Empty<AiModelToolCall>(), new AiUsage(1, 1, 0), AiFinishReason.Stop, "fake-model");

    public static AiModelTurnResult ToolCall(string toolName, string argumentsJson = "{}") =>
        new(
            null,
            new[] { new AiModelToolCall(string.Empty, toolName, JsonDocument.Parse(argumentsJson).RootElement.Clone()) },
            new AiUsage(2, 0, 0),
            AiFinishReason.ToolCalls,
            "fake-model");
}
