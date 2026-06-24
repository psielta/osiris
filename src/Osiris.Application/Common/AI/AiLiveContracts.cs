namespace Osiris.Application.Common.AI;

/// <summary>
/// Provider-neutral realtime (voice) session abstraction. The orchestrator's request/response
/// <see cref="IAiModelClient"/> does not fit a stateful streaming session, so live sessions get their own
/// contract. The Gemini Live WebSocket adapter lives in Infrastructure; Application never sees the wire.
/// </summary>
public interface IAiLiveSessionClient
{
    Task<IAiLiveSession> ConnectAsync(AiLiveSessionRequest request, CancellationToken cancellationToken);
}

/// <summary>A live, bidirectional session. Audio in is 16 kHz PCM16; audio out is 24 kHz PCM16.</summary>
public interface IAiLiveSession : IAsyncDisposable
{
    Task SendAudioAsync(ReadOnlyMemory<byte> pcm16, CancellationToken cancellationToken);

    Task SendTextAsync(string text, CancellationToken cancellationToken);

    /// <summary>Signals the model that the current user audio turn ended (manual end / push-to-talk).</summary>
    Task SignalAudioEndAsync(CancellationToken cancellationToken);

    /// <summary>Feeds tool results back to the model after a <see cref="AiLiveToolCallEvent"/>.</summary>
    Task SendToolResultsAsync(IReadOnlyList<AiModelToolResult> results, CancellationToken cancellationToken);

    /// <summary>The server-side event stream: audio chunks, tool calls, transcripts, turn/goAway signals.</summary>
    IAsyncEnumerable<AiLiveServerEvent> ReadEventsAsync(CancellationToken cancellationToken);
}

public sealed record AiLiveSessionRequest(
    string SystemPrompt,
    IReadOnlyList<AiToolDefinition> Tools,
    AiLiveAudioConfig Audio,
    string CorrelationId,
    string? ResumptionHandle = null);

/// <summary>Voice/output configuration. Output modalities default to audio + transcription.</summary>
public sealed record AiLiveAudioConfig(
    string? VoiceName = null,
    string LanguageCode = "pt-BR",
    bool OutputTranscription = true,
    bool InputTranscription = true);

public abstract record AiLiveServerEvent;

/// <summary>A chunk of the assistant's spoken answer (24 kHz PCM16) to play on the client.</summary>
public sealed record AiLiveAudioChunk(ReadOnlyMemory<byte> Pcm24) : AiLiveServerEvent;

/// <summary>The model requested one or more tools mid-session; execute and reply with tool results.</summary>
public sealed record AiLiveToolCallEvent(IReadOnlyList<AiModelToolCall> Calls) : AiLiveServerEvent;

/// <summary>A live transcript fragment (user or assistant), partial or final.</summary>
public sealed record AiLiveTranscript(string Text, bool IsUser, bool Final) : AiLiveServerEvent;

/// <summary>The model finished its current turn.</summary>
public sealed record AiLiveTurnComplete : AiLiveServerEvent;

/// <summary>The user spoke over the assistant; stop playback and drop buffered audio (barge-in).</summary>
public sealed record AiLiveInterrupted : AiLiveServerEvent;

/// <summary>The server will close the connection soon; reconnect using a resumption handle.</summary>
public sealed record AiLiveGoAway(int MillisLeft) : AiLiveServerEvent;

/// <summary>Provider reported a server-side handle that can resume this session; never expose it to clients.</summary>
public sealed record AiLiveSessionResumptionUpdate(string? Handle, bool Resumable) : AiLiveServerEvent;
