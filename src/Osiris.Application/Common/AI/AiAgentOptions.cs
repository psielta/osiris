namespace Osiris.Application.Common.AI;

/// <summary>
/// Agent-policy configuration (bound from the "AiAssistant" section). These are turn/loop limits and
/// retention values, deliberately separate from the Gemini transport options in Infrastructure.
/// </summary>
public sealed class AiAgentOptions
{
    public const string SectionName = "AiAssistant";

    public string PromptVersion { get; set; } = "osiris-agent-v1.0.0";

    public int MaxToolIterations { get; set; } = 5;

    public int MaxToolCallsPerTurn { get; set; } = 8;

    public int MaxMessageCharacters { get; set; } = 4000;

    public int MaxHistoryMessages { get; set; } = 20;

    public int ConversationRetentionDays { get; set; } = 90;

    public int ProposalTtlMinutes { get; set; } = 15;

    public int MaxConcurrentTurnsPerUser { get; set; } = 1;

    public int DailyTokenLimitPerTenant { get; set; } = 200_000;
}
