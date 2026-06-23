namespace Osiris.Application.Common.AI;

/// <summary>The assembled system prompt plus the version and content hash that identify it.</summary>
public sealed record AiPrompt(string SystemPrompt, string Version, string Hash);

/// <summary>
/// Builds the versioned system prompt for a turn. The version and a hash of the exact text are
/// persisted on every assistant message so a reply can always be traced to the prompt that produced it.
/// </summary>
public interface IAiPromptBuilder
{
    AiPrompt BuildSystemPrompt(AiAgentContext context);
}
