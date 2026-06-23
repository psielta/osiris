using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class AiToolCallExecutorTests
{
    private static readonly JsonElement EmptyArgs = JsonDocument.Parse("{}").RootElement.Clone();

    private static AiAgentContext Context(bool writesEnabled = false) =>
        new(Guid.NewGuid(), "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 23), writesEnabled);

    private static AiToolCallExecutor Create(params IAiTool[] tools) =>
        new(new AiToolRegistry(tools), new AiToolExecutionPolicy(), new NoOpAiDataRedactor(), NullLogger<AiToolCallExecutor>.Instance);

    private static AiModelToolCall Call(string name) => new(string.Empty, name, EmptyArgs);

    [Fact]
    public async Task ExecuteAsync_runs_an_allowed_read_tool()
    {
        var tool = new StubAiTool("read_tool", AiToolRisk.ReadOnly, AiToolResult.Success("{\"v\":1}"));
        var outcome = await Create(tool).ExecuteAsync(Context(), Call("read_tool"), CancellationToken.None);

        Assert.Equal(1, tool.ExecutionCount);
        Assert.Equal(AiToolCallStatus.Succeeded, outcome.Record.Status);
        Assert.Equal("read_tool", outcome.ModelResult.Name);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_unknown_tool()
    {
        var outcome = await Create().ExecuteAsync(Context(), Call("ghost"), CancellationToken.None);

        Assert.Equal(AiToolCallStatus.Rejected, outcome.Record.Status);
        Assert.Equal("unknown_tool", outcome.Record.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_denies_write_tool_when_writes_disabled()
    {
        var tool = new StubAiTool("write_tool", AiToolRisk.WriteProposal);
        var outcome = await Create(tool).ExecuteAsync(Context(writesEnabled: false), Call("write_tool"), CancellationToken.None);

        Assert.Equal(0, tool.ExecutionCount);
        Assert.Equal(AiToolCallStatus.Rejected, outcome.Record.Status);
        Assert.Equal("policy_denied", outcome.Record.ErrorCode);
    }

    [Fact]
    public void Reject_builds_a_rejected_outcome()
    {
        var outcome = Create().Reject(Call("x"), "{\"error\":\"limit\"}", "limit_reached");

        Assert.Equal(AiToolCallStatus.Rejected, outcome.Record.Status);
        Assert.Equal("limit_reached", outcome.Record.ErrorCode);
        Assert.Equal("{\"error\":\"limit\"}", outcome.ModelResult.ResultJson);
    }
}
