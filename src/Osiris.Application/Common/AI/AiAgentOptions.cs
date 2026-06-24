namespace Osiris.Application.Common.AI;

/// <summary>
/// Agent-policy configuration (bound from the "AiAssistant" section). These are turn/loop limits and
/// retention values, deliberately separate from the Gemini transport options in Infrastructure.
/// </summary>
public sealed class AiAgentOptions
{
    public const string SectionName = "AiAssistant";

    public string PromptVersion { get; set; } = "osiris-agent-v1.2.0";

    public int MaxToolIterations { get; set; } = 8;

    public int MaxToolCallsPerTurn { get; set; } = 16;

    public int MaxMessageCharacters { get; set; } = 4000;

    public int MaxHistoryMessages { get; set; } = 20;

    public int ConversationRetentionDays { get; set; } = 90;

    public int ProposalTtlMinutes { get; set; } = 15;

    public int MaxConcurrentTurnsPerUser { get; set; } = 1;

    public int DailyTokenLimitPerTenant { get; set; } = 200_000;

    public int VoiceConnectMaxMinutes { get; set; } = 10;

    public int VoiceSessionMaxMinutes { get; set; } = 30;

    public int VoiceDailyAudioSecondsPerTenant { get; set; } = 1_800;

    public int VoiceMaxConcurrentSessionsPerUser { get; set; } = 1;

    public bool VoiceWritesEnabled { get; set; }

    public int VoiceMaxFrameBytes { get; set; } = 64 * 1024;

    public int VoiceInboundQueueCapacity { get; set; } = 64;

    public int VoiceOutboundQueueCapacity { get; set; } = 64;

    public string[] VoiceAllowedOrigins { get; set; } = Array.Empty<string>();
}
