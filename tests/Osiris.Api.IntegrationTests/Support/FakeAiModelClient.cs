using System.Text.Json;
using Osiris.Application.Common.AI;

namespace Osiris.Api.IntegrationTests.Support;

/// <summary>
/// Deterministic stand-in for the Gemini model client so AI turns exercise the full orchestrator and
/// the real read tool without any network call: the first turn asks for the financial snapshot tool,
/// the next turn (once the tool result is present) returns a final answer.
/// </summary>
public sealed class FakeAiModelClient : IAiModelClient
{
    public Task<AiModelTurnResult> GenerateAsync(AiModelRequest request, CancellationToken cancellationToken)
    {
        var hasToolResult = request.Messages.Any(message => message.Role == AiModelRole.Tool);
        var snapshotOffered = request.Tools.Any(tool => tool.Name == "get_financial_snapshot");

        if (!hasToolResult && snapshotOffered)
        {
            var call = new AiModelToolCall(
                string.Empty,
                "get_financial_snapshot",
                JsonDocument.Parse("{}").RootElement.Clone());

            return Task.FromResult(new AiModelTurnResult(
                null,
                new[] { call },
                new AiUsage(10, 0, 0),
                AiFinishReason.ToolCalls,
                "fake-agent"));
        }

        return Task.FromResult(new AiModelTurnResult(
            "Aqui está o seu panorama financeiro do mês.",
            Array.Empty<AiModelToolCall>(),
            new AiUsage(8, 12, 0),
            AiFinishReason.Stop,
            "fake-agent"));
    }
}
