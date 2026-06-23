using Osiris.Ai.EvaluationTests.Support;
using Osiris.Application.Common.AI;
using Osiris.Domain.Enums;

namespace Osiris.Ai.EvaluationTests;

/// <summary>
/// Data-driven evaluation over the versioned dataset: for each scenario the model is scripted to pick
/// the expected read tool, and we assert the orchestration runs that tool, never the forbidden ones,
/// and surfaces sources when required.
/// </summary>
public sealed class ToolSelectionEvaluationTests
{
    [Theory]
    [MemberData(nameof(EvaluationDataset.CaseIds), MemberType = typeof(EvaluationDataset))]
    public async Task Selected_read_tool_runs_and_forbidden_tools_never_execute(string id)
    {
        var scenario = EvaluationDataset.Get(id);

        var expected = new RecordingTool(scenario.ExpectedTool, AiToolRisk.ReadOnly, scenario.RequireSources);
        var forbidden = scenario.ForbiddenTools
            .Select(name => new RecordingTool(name, AiToolRisk.Forbidden))
            .ToList();

        var tools = new List<IAiTool> { expected };
        tools.AddRange(forbidden);

        var orchestrator = EvaluationHarness.CreateOrchestrator(
            new ScriptedAiModelClient(scenario.ExpectedTool),
            tools.ToArray());

        var result = await orchestrator.RunAsync(
            EvaluationHarness.Context(),
            Array.Empty<AiModelMessage>(),
            scenario.Question,
            CancellationToken.None);

        Assert.Equal(1, expected.ExecutionCount);
        Assert.All(forbidden, tool => Assert.Equal(0, tool.ExecutionCount));
        Assert.Contains(result.ExecutedToolCalls, call =>
            call.ToolName == scenario.ExpectedTool && call.Status == AiToolCallStatus.Succeeded);

        if (scenario.RequireSources)
        {
            Assert.NotEmpty(result.Sources);
        }
    }
}
