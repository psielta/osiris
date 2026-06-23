using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class AiToolRegistryTests
{
    private readonly StubAiTool _read = new("read_tool", AiToolRisk.ReadOnly);
    private readonly StubAiTool _write = new("write_tool", AiToolRisk.WriteProposal);
    private readonly StubAiTool _forbidden = new("forbidden_tool", AiToolRisk.Forbidden);

    private AiToolRegistry CreateRegistry() => new(new IAiTool[] { _read, _write, _forbidden });

    private static AiAgentContext Context(bool writesEnabled) =>
        new(Guid.NewGuid(), "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 22), writesEnabled);

    [Fact]
    public void GetAllowedTools_WithWritesDisabled_OffersOnlyReadTools()
    {
        var allowed = CreateRegistry().GetAllowedTools(Context(writesEnabled: false));

        Assert.Contains(allowed, tool => tool.Name == "read_tool");
        Assert.DoesNotContain(allowed, tool => tool.Name == "write_tool");
        Assert.DoesNotContain(allowed, tool => tool.Name == "forbidden_tool");
    }

    [Fact]
    public void GetAllowedTools_WithWritesEnabled_OffersWriteProposalsButNeverForbidden()
    {
        var allowed = CreateRegistry().GetAllowedTools(Context(writesEnabled: true));

        Assert.Contains(allowed, tool => tool.Name == "read_tool");
        Assert.Contains(allowed, tool => tool.Name == "write_tool");
        Assert.DoesNotContain(allowed, tool => tool.Name == "forbidden_tool");
    }

    [Fact]
    public void Find_ReturnsToolByExactName_OrNull()
    {
        var registry = CreateRegistry();

        Assert.Same(_read, registry.Find("read_tool"));
        Assert.Null(registry.Find("unknown_tool"));
    }
}
