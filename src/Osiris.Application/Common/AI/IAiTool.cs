using System.Text.Json;
using Osiris.Domain.Enums;

namespace Osiris.Application.Common.AI;

/// <summary>
/// A single capability the agent can use. Tools are a closed, whitelisted catalogue: each declares a
/// strict JSON-schema for its inputs and its <see cref="Risk"/>. Read tools run during a turn; write
/// tools only ever create proposals. Tenant scope is never an input — it comes from the server context.
/// </summary>
public interface IAiTool
{
    string Name { get; }

    string Description { get; }

    AiToolRisk Risk { get; }

    /// <summary>JSON-schema object describing the accepted arguments (serialized for the model).</summary>
    object InputSchema { get; }

    Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken);
}
