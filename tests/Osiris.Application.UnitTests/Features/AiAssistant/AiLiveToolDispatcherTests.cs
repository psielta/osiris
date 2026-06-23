using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class AiLiveToolDispatcherTests
{
    private static readonly JsonElement EmptyArgs = JsonDocument.Parse("{}").RootElement.Clone();

    private static AiAgentContext Context() =>
        new(Guid.NewGuid(), "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 23), WritesEnabled: false);

    private static AiModelToolCall Call(string name) => new(string.Empty, name, EmptyArgs);

    private static AiLiveToolDispatcher Create(params IAiTool[] tools)
    {
        var executor = new AiToolCallExecutor(
            new AiToolRegistry(tools), new AiToolExecutionPolicy(), new NoOpAiDataRedactor(),
            NullLogger<AiToolCallExecutor>.Instance);
        return new AiLiveToolDispatcher(executor);
    }

    [Fact]
    public async Task DispatchAsync_executes_each_call_and_dedups_sources()
    {
        var source = new AiSource("account", "a1", "Conta");
        var tool = new StubAiTool("read_tool", AiToolRisk.ReadOnly, AiToolResult.Success("{}", new[] { source }));
        var dispatcher = Create(tool);

        var batch = await dispatcher.DispatchAsync(
            Context(),
            new[] { Call("read_tool"), Call("read_tool") },
            CancellationToken.None);

        Assert.Equal(2, batch.Results.Count);
        Assert.Equal(2, batch.Records.Count);
        Assert.Equal(2, tool.ExecutionCount);
        Assert.Single(batch.Sources); // same (type,id) collapsed
    }

    [Fact]
    public async Task DispatchAsync_keeps_unknown_tool_as_rejected_result()
    {
        var batch = await Create().DispatchAsync(Context(), new[] { Call("ghost") }, CancellationToken.None);

        Assert.Single(batch.Results);
        Assert.Equal("unknown_tool", Assert.Single(batch.Records).ErrorCode);
    }
}
