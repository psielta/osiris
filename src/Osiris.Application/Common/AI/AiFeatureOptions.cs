namespace Osiris.Application.Common.AI;

/// <summary>
/// Feature switches for the AI assistant (bound from the "Features" section). Everything is off by
/// default: with the assistant disabled the rest of Osiris is completely unaffected.
/// </summary>
public sealed class AiFeatureOptions
{
    public const string SectionName = "Features";

    /// <summary>Master switch: when false the assistant endpoints behave as if they do not exist.</summary>
    public bool AiAssistant { get; set; }

    /// <summary>Allows write-proposal tools to be offered. Read-only stays available without this.</summary>
    public bool AiAssistantWrites { get; set; }

    /// <summary>Separate switch for the mobile surface, independent from the web one.</summary>
    public bool AiAssistantMobile { get; set; }

    /// <summary>Realtime voice agent (Gemini Live API). Off by default; requires <see cref="AiAssistant"/>.</summary>
    public bool AiAssistantVoice { get; set; }
}
