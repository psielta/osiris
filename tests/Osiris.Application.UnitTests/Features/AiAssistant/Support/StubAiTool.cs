using System.Text.Json;
using Osiris.Application.Common.AI;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant.Support;

internal sealed class StubAiTool : IAiTool
{
    private readonly AiToolResult _result;

    public StubAiTool(string name, AiToolRisk risk, AiToolResult? result = null)
    {
        Name = name;
        Risk = risk;
        _result = result ?? AiToolResult.Success("{\"ok\":true}");
    }

    public string Name { get; }

    public string Description => "stub tool";

    public AiToolRisk Risk { get; }

    public object InputSchema => new { type = "object" };

    public int ExecutionCount { get; private set; }

    public Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.FromResult(_result);
    }
}
