using Osiris.Application.Common.AI;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Services;

/// <summary>
/// Whitelist of tools the agent can use, materialized from DI. The offered set is filtered by risk:
/// read-only tools are always available; write proposals require the writes feature; restricted and
/// forbidden tools are never offered.
/// </summary>
public sealed class AiToolRegistry : IAiToolRegistry
{
    private readonly IReadOnlyList<IAiTool> _tools;

    public AiToolRegistry(IEnumerable<IAiTool> tools)
    {
        _tools = tools.ToList();
    }

    public IReadOnlyCollection<IAiTool> GetAllowedTools(AiAgentContext context)
    {
        return _tools.Where(tool => IsOffered(tool, context)).ToList();
    }

    public IAiTool? Find(string name)
    {
        return _tools.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.Ordinal));
    }

    private static bool IsOffered(IAiTool tool, AiAgentContext context)
    {
        return tool.Risk switch
        {
            AiToolRisk.ReadOnly => true,
            AiToolRisk.WriteProposal => context.WritesEnabled,
            _ => false
        };
    }
}
