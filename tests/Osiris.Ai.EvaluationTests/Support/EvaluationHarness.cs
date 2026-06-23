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
        return new AiAgentOrchestrator(
            modelClient,
            new AiToolRegistry(tools),
            new AiToolExecutionPolicy(),
            new AiPromptBuilder(options),
            new NoOpRedactor(),
            options,
            NullLogger<AiAgentOrchestrator>.Instance);
    }
}
