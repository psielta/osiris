namespace Osiris.Application.Common.AI;

/// <summary>
/// Resolves the set of tools the agent may use. The allowed set depends on the server context
/// (e.g. whether write proposals are enabled), never on the model's request.
/// </summary>
public interface IAiToolRegistry
{
    IReadOnlyCollection<IAiTool> GetAllowedTools(AiAgentContext context);

    IAiTool? Find(string name);
}
