using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class AiAgentOrchestratorTests
{
    private static AiAgentContext Context(bool writesEnabled = false) =>
        new(Guid.NewGuid(), "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 22), writesEnabled);

    private static AiAgentOrchestrator CreateOrchestrator(FakeAiModelClient model, params IAiTool[] tools)
    {
        var options = Options.Create(new AiAgentOptions());
        var registry = new AiToolRegistry(tools);
        var executor = new AiToolCallExecutor(
            registry, new AiToolExecutionPolicy(), new NoOpAiDataRedactor(), NullLogger<AiToolCallExecutor>.Instance);
        return new AiAgentOrchestrator(
            model,
            registry,
            executor,
            new AiPromptBuilder(options),
            options,
            NullLogger<AiAgentOrchestrator>.Instance);
    }

    [Fact]
    public async Task RunAsync_WithNoToolCalls_ReturnsModelText()
    {
        var model = new FakeAiModelClient(FakeAiModelClient.Text("Olá!"));
        var orchestrator = CreateOrchestrator(model);

        var result = await orchestrator.RunAsync(Context(), Array.Empty<AiModelMessage>(), "oi", CancellationToken.None);

        Assert.Equal("Olá!", result.AssistantText);
        Assert.Empty(result.ExecutedToolCalls);
        Assert.Single(model.Requests);
    }

    [Fact]
    public async Task RunAsync_ExecutesAllowedTool_ThenReturnsFinalText()
    {
        var tool = new StubAiTool("read_tool", AiToolRisk.ReadOnly, AiToolResult.Success("{\"value\":1}"));
        var model = new FakeAiModelClient(FakeAiModelClient.ToolCall("read_tool"), FakeAiModelClient.Text("Pronto."));
        var orchestrator = CreateOrchestrator(model, tool);

        var result = await orchestrator.RunAsync(Context(), Array.Empty<AiModelMessage>(), "pergunta", CancellationToken.None);

        Assert.Equal("Pronto.", result.AssistantText);
        Assert.Equal(1, tool.ExecutionCount);
        var record = Assert.Single(result.ExecutedToolCalls);
        Assert.Equal("read_tool", record.ToolName);
        Assert.Equal(AiToolCallStatus.Succeeded, record.Status);
        Assert.Equal(2, model.Requests.Count);
    }

    [Fact]
    public async Task RunAsync_DoesNotExecuteWriteTool_WhenWritesDisabled()
    {
        var tool = new StubAiTool("write_tool", AiToolRisk.WriteProposal);
        var model = new FakeAiModelClient(FakeAiModelClient.ToolCall("write_tool"), FakeAiModelClient.Text("ok"));
        var orchestrator = CreateOrchestrator(model, tool);

        var result = await orchestrator.RunAsync(
            Context(writesEnabled: false),
            Array.Empty<AiModelMessage>(),
            "crie algo",
            CancellationToken.None);

        Assert.Equal(0, tool.ExecutionCount);
        var record = Assert.Single(result.ExecutedToolCalls);
        Assert.Equal(AiToolCallStatus.Rejected, record.Status);
        Assert.Equal("policy_denied", record.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_RejectsUnknownTool_AndStillAnswers()
    {
        var model = new FakeAiModelClient(FakeAiModelClient.ToolCall("ghost_tool"), FakeAiModelClient.Text("ok"));
        var orchestrator = CreateOrchestrator(model);

        var result = await orchestrator.RunAsync(Context(), Array.Empty<AiModelMessage>(), "x", CancellationToken.None);

        Assert.Equal("ok", result.AssistantText);
        var record = Assert.Single(result.ExecutedToolCalls);
        Assert.Equal(AiToolCallStatus.Rejected, record.Status);
        Assert.Equal("unknown_tool", record.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WhenModelNeverStops_FallsBackWithinIterationBudget()
    {
        var tool = new StubAiTool("read_tool", AiToolRisk.ReadOnly);
        var model = new FakeAiModelClient(FakeAiModelClient.ToolCall("read_tool")) { RepeatLast = true };
        var orchestrator = CreateOrchestrator(model, tool);

        var result = await orchestrator.RunAsync(Context(), Array.Empty<AiModelMessage>(), "loop", CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.AssistantText));
        Assert.Equal(AiFinishReason.Other, result.FinishReason);
        // The loop is bounded by AiAgentOptions.MaxToolIterations (default 8).
        Assert.Equal(8, model.Requests.Count);
    }
}
