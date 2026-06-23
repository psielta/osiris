using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class AiToolExecutionPolicyTests
{
    private readonly AiToolExecutionPolicy _policy = new();

    private static AiAgentContext Context(bool writesEnabled) =>
        new(Guid.NewGuid(), "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 22), writesEnabled);

    [Fact]
    public void ReadOnlyTool_IsAlwaysAllowed()
    {
        var decision = _policy.Evaluate(Context(writesEnabled: false), new StubAiTool("read", AiToolRisk.ReadOnly));

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void WriteProposalTool_IsDenied_WhenWritesDisabled()
    {
        var decision = _policy.Evaluate(Context(writesEnabled: false), new StubAiTool("write", AiToolRisk.WriteProposal));

        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public void WriteProposalTool_IsAllowed_WhenWritesEnabled()
    {
        var decision = _policy.Evaluate(Context(writesEnabled: true), new StubAiTool("write", AiToolRisk.WriteProposal));

        Assert.True(decision.IsAllowed);
    }

    [Theory]
    [InlineData(AiToolRisk.Restricted)]
    [InlineData(AiToolRisk.Forbidden)]
    public void RestrictedAndForbiddenTools_AreNeverAllowed(AiToolRisk risk)
    {
        var decision = _policy.Evaluate(Context(writesEnabled: true), new StubAiTool("blocked", risk));

        Assert.False(decision.IsAllowed);
    }
}
