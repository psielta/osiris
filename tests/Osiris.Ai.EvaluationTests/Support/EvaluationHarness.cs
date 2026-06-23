using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Services;

namespace Osiris.Ai.EvaluationTests.Support;

internal static class EvaluationHarness
{
    public static AiAgentContext Context(bool writesEnabled = false) =>
        new(Guid.NewGuid(), "user-eval", Guid.NewGuid(), "corr-eval", new DateOnly(2026, 6, 22), writesEnabled);

    public static AiAgentOrchestrator CreateOrchestrator(IAiModelClient modelClient, params IAiTool[] tools)
    {
        var options = Options.Create(new AiAgentOptions());
        var registry = new AiToolRegistry(tools);
        var executor = new AiToolCallExecutor(
            registry, new AiToolExecutionPolicy(), new NoOpRedactor(), NullLogger<AiToolCallExecutor>.Instance);
        return new AiAgentOrchestrator(
            modelClient,
            registry,
            executor,
            new AiPromptBuilder(options),
            options,
            NullLogger<AiAgentOrchestrator>.Instance);
    }
}
