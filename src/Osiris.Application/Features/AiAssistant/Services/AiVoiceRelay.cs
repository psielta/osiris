using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.AiAssistant.Services;

public sealed record AiVoiceRelayRequest(
    Guid TenantId,
    string UserId,
    DateOnly Today,
    Guid? ConversationId,
    bool WritesEnabled,
    string CorrelationId);

/// <summary>
/// Transport-agnostic relay between a client WebSocket and a Gemini Live session, shared by the Web
/// (cookie) and API (JWT) voice endpoints. Streams audio both ways and executes tool calls through the
/// shared <see cref="IAiLiveToolDispatcher"/>.
/// </summary>
public sealed class AiVoiceRelay
{
    private const int InputBytesPerSecond = 16_000 * 2;
    private const int OutputBytesPerSecond = 24_000 * 2;
    private const string VoiceConversationTitle = "Conversa por voz";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IAiLiveSessionClient _liveClient;
    private readonly IAiLiveToolDispatcher _dispatcher;
    private readonly IAiPromptBuilder _promptBuilder;
    private readonly IAiToolRegistry _toolRegistry;
    private readonly IAiConversationRepository _conversations;
    private readonly IAiVoiceSessionLimiter _sessionLimiter;
    private readonly IAiVoiceResumptionStore _resumptionStore;
    private readonly IDateTimeProvider _clock;
    private readonly AiAgentOptions _options;
    private readonly AiVoiceTelemetry _telemetry;
    private readonly ILogger<AiVoiceRelay> _logger;

    public AiVoiceRelay(
        IAiLiveSessionClient liveClient,
        IAiLiveToolDispatcher dispatcher,
        IAiPromptBuilder promptBuilder,
        IAiToolRegistry toolRegistry,
        IAiConversationRepository conversations,
        IAiVoiceSessionLimiter sessionLimiter,
        IAiVoiceResumptionStore resumptionStore,
        IDateTimeProvider clock,
        IOptions<AiAgentOptions> options,
        AiVoiceTelemetry telemetry,
        ILogger<AiVoiceRelay> logger)
    {
        _liveClient = liveClient;
        _dispatcher = dispatcher;
        _promptBuilder = promptBuilder;
        _toolRegistry = toolRegistry;
        _conversations = conversations;
        _sessionLimiter = sessionLimiter;
        _resumptionStore = resumptionStore;
        _clock = clock;
        _options = options.Value;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task RunAsync(WebSocket socket, AiVoiceRelayRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            await TrySendJsonAsync(socket, new { type = "error", message = "Usuário não autenticado." }, cancellationToken);
            await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "unauthenticated");
            return;
        }

        if (!_sessionLimiter.TryAcquire(
            request.TenantId,
            request.UserId,
            _options.VoiceMaxConcurrentSessionsPerUser,
            out var lease))
        {
            _telemetry.SessionRejected(request.TenantId, "concurrent_limit");
            await TrySendJsonAsync(socket, new
            {
                type = "error",
                message = "Já existe uma sessão de voz ativa para este usuário."
            }, cancellationToken);
            await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "concurrent_limit");
            return;
        }

        using (lease)
        {
            var startedAt = _clock.UtcNow;
            _telemetry.SessionStarted(request.TenantId);

            try
            {
                await RunLeasedAsync(socket, request, cancellationToken);
            }
            finally
            {
                _telemetry.SessionDuration(request.TenantId, _clock.UtcNow - startedAt);
            }
        }
    }

    private async Task RunLeasedAsync(WebSocket socket, AiVoiceRelayRequest request, CancellationToken cancellationToken)
    {
        var conversation = await ResolveConversationAsync(request, cancellationToken);
        var context = new AiAgentContext(
            request.TenantId,
            request.UserId,
            conversation.Id,
            request.CorrelationId,
            request.Today,
            request.WritesEnabled);

        var prompt = _promptBuilder.BuildSystemPrompt(context);
        var tools = _toolRegistry.GetAllowedTools(context)
            .Select(tool => new AiToolDefinition(tool.Name, tool.Description, tool.InputSchema, tool.Risk))
            .ToList();

        var utcNow = _clock.UtcNow;
        var dayStartUtc = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
        var usedVoiceSeconds = _options.VoiceDailyAudioSecondsPerTenant > 0
            ? await _conversations.SumVoiceInputSecondsSinceAsync(request.TenantId, dayStartUtc, cancellationToken)
            : 0;

        if (_options.VoiceDailyAudioSecondsPerTenant > 0
            && usedVoiceSeconds >= _options.VoiceDailyAudioSecondsPerTenant)
        {
            _telemetry.SessionRejected(request.TenantId, "audio_budget");
            await TrySendJsonAsync(socket, new
            {
                type = "error",
                message = "Limite diário de áudio do assistente atingido. Tente novamente amanhã."
            }, cancellationToken);
            await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "audio_budget");
            return;
        }

        var state = new VoiceSessionState(conversation, context, prompt, usedVoiceSeconds);
        await TrySendJsonAsync(socket, new
        {
            type = "session",
            conversationId = conversation.Id,
            writesEnabled = request.WritesEnabled
        }, cancellationToken);

        using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_options.VoiceSessionMaxMinutes > 0)
        {
            sessionCts.CancelAfter(TimeSpan.FromMinutes(_options.VoiceSessionMaxMinutes));
        }

        var inbound = Channel.CreateBounded<VoiceInboundFrame>(new BoundedChannelOptions(Math.Max(1, _options.VoiceInboundQueueCapacity))
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });
        var outbound = Channel.CreateBounded<VoiceOutboundFrame>(new BoundedChannelOptions(Math.Max(1, _options.VoiceOutboundQueueCapacity))
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        var receiveTask = ReceiveClientAsync(socket, inbound.Writer, outbound.Writer, state, sessionCts.Token);
        var sendTask = SendClientAsync(socket, outbound.Reader, sessionCts.Token);
        var liveTask = RunLiveLoopAsync(inbound.Reader, outbound.Writer, state, tools, sessionCts.Token);

        await Task.WhenAny(receiveTask, sendTask, liveTask);
        sessionCts.Cancel();
        inbound.Writer.TryComplete();
        outbound.Writer.TryComplete();

        await Task.WhenAll(Swallow(receiveTask), Swallow(sendTask), Swallow(liveTask));
        await FlushVoiceTurnAsync(state, "SessionClosed", CancellationToken.None);
        await CloseAsync(socket, WebSocketCloseStatus.NormalClosure, "done");
    }

    private async Task<AiConversation> ResolveConversationAsync(
        AiVoiceRelayRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ConversationId is { } conversationId)
        {
            var existing = await _conversations.GetAsync(
                request.TenantId,
                request.UserId,
                conversationId,
                cancellationToken);

            if (existing is not null && existing.IsActive)
            {
                return existing;
            }
        }

        var conversation = new AiConversation(
            request.TenantId,
            request.UserId,
            VoiceConversationTitle,
            _options.PromptVersion);

        await _conversations.AddAsync(conversation, cancellationToken);
        return conversation;
    }

    private async Task RunLiveLoopAsync(
        ChannelReader<VoiceInboundFrame> inbound,
        ChannelWriter<VoiceOutboundFrame> outbound,
        VoiceSessionState state,
        IReadOnlyList<AiToolDefinition> tools,
        CancellationToken cancellationToken)
    {
        string? resumeHandle = _resumptionStore.Get(
            state.Context.TenantId,
            state.Context.UserId,
            state.Context.ConversationId,
            _clock.UtcNow);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var session = await _liveClient.ConnectAsync(
                    new AiLiveSessionRequest(
                        state.Prompt.SystemPrompt,
                        tools,
                        new AiLiveAudioConfig(),
                        state.Context.CorrelationId,
                        resumeHandle),
                    cancellationToken);

                using var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (_options.VoiceConnectMaxMinutes > 0)
                {
                    connectionCts.CancelAfter(TimeSpan.FromMinutes(_options.VoiceConnectMaxMinutes));
                }

                var sendTask = SendInboundToGeminiAsync(inbound, session, connectionCts.Token);
                var readTask = ReadGeminiToClientAsync(outbound, session, state, connectionCts.Token);
                var completed = await Task.WhenAny(sendTask, readTask);

                connectionCts.Cancel();
                await Task.WhenAll(Swallow(sendTask), Swallow(readTask));

                if (completed == sendTask || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                resumeHandle = _resumptionStore.Get(
                    state.Context.TenantId,
                    state.Context.UserId,
                    state.Context.ConversationId,
                    _clock.UtcNow);

                if (string.IsNullOrWhiteSpace(resumeHandle))
                {
                    return;
                }

                await QueueJsonAsync(outbound, new { type = "status", value = "reconnecting" }, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Voice live loop failed for {CorrelationId}.", state.Context.CorrelationId);
                await QueueJsonAsync(outbound, new { type = "error", message = "Sessão de voz indisponível." }, cancellationToken);
                return;
            }
        }
    }

    private async Task ReceiveClientAsync(
        WebSocket socket,
        ChannelWriter<VoiceInboundFrame> inbound,
        ChannelWriter<VoiceOutboundFrame> outbound,
        VoiceSessionState state,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var message = new MemoryStream();

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                message.SetLength(0);
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    if (message.Length + result.Count > _options.VoiceMaxFrameBytes)
                    {
                        await QueueJsonAsync(outbound, new
                        {
                            type = "error",
                            message = "Frame de áudio muito grande."
                        }, cancellationToken);
                        return;
                    }

                    message.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var audio = message.ToArray();
                    var milliseconds = AudioMilliseconds(audio.Length, InputBytesPerSecond);
                    if (!state.TryAddInputAudio(milliseconds, _options.VoiceDailyAudioSecondsPerTenant))
                    {
                        _telemetry.SessionRejected(state.Context.TenantId, "audio_budget");
                        await QueueJsonAsync(outbound, new
                        {
                            type = "error",
                            message = "Limite diário de áudio do assistente atingido. Tente novamente amanhã."
                        }, cancellationToken);
                        return;
                    }

                    _telemetry.AudioInput(state.Context.TenantId, milliseconds);
                    await inbound.WriteAsync(VoiceInboundFrame.FromAudio(audio), cancellationToken);
                }
                else if (!await HandleControlAsync(inbound, message, cancellationToken))
                {
                    return;
                }
            }
        }
        finally
        {
            inbound.TryComplete();
        }
    }

    private static async Task<bool> HandleControlAsync(
        ChannelWriter<VoiceInboundFrame> inbound,
        MemoryStream message,
        CancellationToken cancellationToken)
    {
        string? type;
        string? content;
        try
        {
            using var document = JsonDocument.Parse(message.GetBuffer().AsMemory(0, (int)message.Length));
            type = document.RootElement.TryGetProperty("type", out var t) ? t.GetString() : null;
            content = document.RootElement.TryGetProperty("content", out var c) ? c.GetString() : null;
        }
        catch (JsonException)
        {
            return true;
        }

        switch (type)
        {
            case "stop":
                return false;
            case "audioEnd":
                await inbound.WriteAsync(VoiceInboundFrame.FromAudioEnd(), cancellationToken);
                return true;
            case "text" when !string.IsNullOrWhiteSpace(content):
                await inbound.WriteAsync(VoiceInboundFrame.FromText(content!), cancellationToken);
                return true;
            default:
                return true;
        }
    }

    private static async Task SendInboundToGeminiAsync(
        ChannelReader<VoiceInboundFrame> inbound,
        IAiLiveSession session,
        CancellationToken cancellationToken)
    {
        await foreach (var frame in inbound.ReadAllAsync(cancellationToken))
        {
            switch (frame.Kind)
            {
                case VoiceInboundKind.Audio when frame.Audio is not null:
                    await session.SendAudioAsync(frame.Audio, cancellationToken);
                    break;
                case VoiceInboundKind.AudioEnd:
                    await session.SignalAudioEndAsync(cancellationToken);
                    break;
                case VoiceInboundKind.Text when !string.IsNullOrWhiteSpace(frame.Text):
                    await session.SendTextAsync(frame.Text, cancellationToken);
                    break;
            }
        }
    }

    private async Task ReadGeminiToClientAsync(
        ChannelWriter<VoiceOutboundFrame> outbound,
        IAiLiveSession session,
        VoiceSessionState state,
        CancellationToken cancellationToken)
    {
        await foreach (var serverEvent in session.ReadEventsAsync(cancellationToken))
        {
            switch (serverEvent)
            {
                case AiLiveAudioChunk audio:
                    var bytes = audio.Pcm24.ToArray();
                    var milliseconds = AudioMilliseconds(bytes.Length, OutputBytesPerSecond);
                    state.AddOutputAudio(milliseconds);
                    _telemetry.AudioOutput(state.Context.TenantId, milliseconds);
                    if (!outbound.TryWrite(VoiceOutboundFrame.FromAudio(bytes)))
                    {
                        _telemetry.FrameDropped(state.Context.TenantId, "outbound");
                    }

                    break;

                case AiLiveTranscript transcript:
                    state.AppendTranscript(transcript);
                    await QueueJsonAsync(outbound, new
                    {
                        type = "transcript",
                        role = transcript.IsUser ? "user" : "assistant",
                        text = transcript.Text,
                        final = transcript.Final
                    }, cancellationToken);
                    break;

                case AiLiveToolCallEvent toolCall:
                    var sw = Stopwatch.StartNew();
                    var batch = await _dispatcher.DispatchAsync(state.Context, toolCall.Calls, cancellationToken);
                    sw.Stop();
                    state.AddToolRecords(batch.Records);
                    foreach (var record in batch.Records)
                    {
                        _telemetry.ToolLatency(state.Context.TenantId, record.ToolName, record.DurationMs);
                    }

                    await session.SendToolResultsAsync(batch.Results, cancellationToken);
                    if (batch.Sources.Count > 0)
                    {
                        await QueueJsonAsync(outbound, new { type = "sources", items = batch.Sources }, cancellationToken);
                    }

                    foreach (var proposal in batch.Proposals)
                    {
                        await QueueJsonAsync(outbound, new
                        {
                            type = "proposal",
                            proposal = MapProposal(proposal)
                        }, cancellationToken);
                    }

                    _logger.LogInformation(
                        "Voice dispatched {ToolCount} tool calls in {ElapsedMs} ms for {CorrelationId}.",
                        toolCall.Calls.Count,
                        sw.ElapsedMilliseconds,
                        state.Context.CorrelationId);
                    break;

                case AiLiveInterrupted:
                    await QueueJsonAsync(outbound, new { type = "status", value = "interrupted" }, cancellationToken);
                    break;

                case AiLiveTurnComplete:
                    await FlushVoiceTurnAsync(state, "TurnComplete", cancellationToken);
                    await QueueJsonAsync(outbound, new { type = "status", value = "idle" }, cancellationToken);
                    break;

                case AiLiveGoAway goAway:
                    await QueueJsonAsync(outbound, new
                    {
                        type = "status",
                        value = "goingaway",
                        millisLeft = goAway.MillisLeft,
                        conversationId = state.Context.ConversationId
                    }, cancellationToken);
                    break;

                case AiLiveSessionResumptionUpdate update:
                    if (update.Resumable && !string.IsNullOrWhiteSpace(update.Handle))
                    {
                        _resumptionStore.Save(
                            state.Context.TenantId,
                            state.Context.UserId,
                            state.Context.ConversationId,
                            update.Handle,
                            _clock.UtcNow.AddHours(2));
                    }

                    break;
            }
        }
    }

    private async Task SendClientAsync(
        WebSocket socket,
        ChannelReader<VoiceOutboundFrame> outbound,
        CancellationToken cancellationToken)
    {
        await foreach (var frame in outbound.ReadAllAsync(cancellationToken))
        {
            if (socket.State != WebSocketState.Open)
            {
                return;
            }

            if (frame.Audio is not null)
            {
                await socket.SendAsync(frame.Audio.AsMemory(), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);
            }
            else if (frame.Json is not null)
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(frame.Json, Json);
                await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            }
        }
    }

    private async Task FlushVoiceTurnAsync(
        VoiceSessionState state,
        string finishReason,
        CancellationToken cancellationToken)
    {
        var snapshot = state.DrainTurn();
        if (snapshot.IsEmpty)
        {
            return;
        }

        var messages = new List<AiMessage>(2);
        if (!string.IsNullOrWhiteSpace(snapshot.UserText))
        {
            messages.Add(AiMessage.ForVoiceUser(
                state.Context.TenantId,
                state.Context.ConversationId,
                state.Context.UserId,
                snapshot.UserText,
                snapshot.InputAudioMilliseconds));

            if (state.Conversation.Title == VoiceConversationTitle)
            {
                state.Conversation.Rename(BuildTitle(snapshot.UserText), _clock.UtcNow);
            }
        }

        var assistantText = snapshot.AssistantText;
        if (string.IsNullOrWhiteSpace(assistantText) && snapshot.ToolRecords.Count > 0)
        {
            assistantText = "Proposta criada por voz. Confirme ou rejeite na tela.";
        }

        AiMessage? assistantMessage = null;
        if (!string.IsNullOrWhiteSpace(assistantText) || snapshot.ToolRecords.Count > 0)
        {
            assistantMessage = AiMessage.ForAssistant(
                state.Context.TenantId,
                state.Context.ConversationId,
                state.Context.UserId,
                assistantText,
                model: null,
                state.Prompt.Version,
                state.Prompt.Hash,
                inputTokens: 0,
                outputTokens: 0,
                cachedTokens: 0,
                latencyMs: 0,
                finishReason,
                state.Context.CorrelationId,
                channel: "voice",
                inputAudioMilliseconds: 0,
                outputAudioMilliseconds: snapshot.OutputAudioMilliseconds);
            messages.Add(assistantMessage);
        }

        var utcNow = _clock.UtcNow;
        var toolCalls = snapshot.ToolRecords
            .Select(record => new AiToolCall(
                state.Context.TenantId,
                state.Context.ConversationId,
                assistantMessage?.Id ?? Guid.NewGuid(),
                record.ToolName,
                record.Risk,
                record.Status,
                record.ArgumentsJsonRedacted,
                record.ResultJsonRedacted,
                record.DurationMs,
                record.ErrorCode,
                utcNow))
            .ToList();

        state.Conversation.Touch(utcNow);
        await _conversations.SaveTurnAsync(state.Conversation, messages, toolCalls, cancellationToken);
    }

    private static async Task QueueJsonAsync(
        ChannelWriter<VoiceOutboundFrame> outbound,
        object payload,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!outbound.TryWrite(VoiceOutboundFrame.FromJson(payload)))
            {
                await outbound.WriteAsync(VoiceOutboundFrame.FromJson(payload), cancellationToken);
            }
        }
        catch (Exception exception) when (exception is ChannelClosedException or OperationCanceledException)
        {
        }
    }

    private static async Task TrySendJsonAsync(WebSocket socket, object payload, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
        }
    }

    private static object MapProposal(AiProposalReference proposal) => new
    {
        id = proposal.Id,
        actionType = proposal.ActionType,
        displaySummary = proposal.DisplaySummary,
        impactSummary = proposal.ImpactSummary,
        riskLevel = proposal.RiskLevel,
        status = "Pending",
        expiresAtUtc = proposal.ExpiresAtUtc
    };

    private static int AudioMilliseconds(int byteCount, int bytesPerSecond) =>
        byteCount <= 0 ? 0 : (int)Math.Ceiling(byteCount * 1000d / bytesPerSecond);

    private static string BuildTitle(string text)
    {
        var title = text.Trim();
        var newline = title.IndexOf('\n');
        if (newline >= 0)
        {
            title = title[..newline].Trim();
        }

        return title.Length > 80 ? title[..80] : title;
    }

    private static async Task Swallow(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception exception) when (exception is OperationCanceledException or WebSocketException or ObjectDisposedException or ChannelClosedException)
        {
        }
    }

    private static async Task CloseAsync(WebSocket socket, WebSocketCloseStatus status, string description)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseAsync(status, description, CancellationToken.None);
            }
            catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException or OperationCanceledException)
            {
            }
        }
    }

    private sealed class VoiceSessionState
    {
        private readonly object _gate = new();
        private readonly List<AiToolCallRecord> _toolRecords = new();
        private string _userText = string.Empty;
        private string _assistantText = string.Empty;
        private int _turnInputAudioMilliseconds;
        private int _turnOutputAudioMilliseconds;
        private int _sessionInputAudioMilliseconds;

        public VoiceSessionState(
            AiConversation conversation,
            AiAgentContext context,
            AiPrompt prompt,
            long persistedInputSeconds)
        {
            Conversation = conversation;
            Context = context;
            Prompt = prompt;
            PersistedInputSeconds = persistedInputSeconds;
        }

        public AiConversation Conversation { get; }
        public AiAgentContext Context { get; }
        public AiPrompt Prompt { get; }
        public long PersistedInputSeconds { get; }

        public bool TryAddInputAudio(int milliseconds, int dailyLimitSeconds)
        {
            lock (_gate)
            {
                var totalSeconds = PersistedInputSeconds
                    + (long)Math.Ceiling((_sessionInputAudioMilliseconds + milliseconds) / 1000d);

                if (dailyLimitSeconds > 0 && totalSeconds > dailyLimitSeconds)
                {
                    return false;
                }

                _sessionInputAudioMilliseconds += milliseconds;
                _turnInputAudioMilliseconds += milliseconds;
                return true;
            }
        }

        public void AddOutputAudio(int milliseconds)
        {
            lock (_gate)
            {
                _turnOutputAudioMilliseconds += milliseconds;
            }
        }

        public void AppendTranscript(AiLiveTranscript transcript)
        {
            lock (_gate)
            {
                if (transcript.IsUser)
                {
                    _userText += transcript.Text;
                }
                else
                {
                    _assistantText += transcript.Text;
                }
            }
        }

        public void AddToolRecords(IReadOnlyList<AiToolCallRecord> records)
        {
            lock (_gate)
            {
                _toolRecords.AddRange(records);
            }
        }

        public VoiceTurnSnapshot DrainTurn()
        {
            lock (_gate)
            {
                var snapshot = new VoiceTurnSnapshot(
                    _userText.Trim(),
                    _assistantText.Trim(),
                    _turnInputAudioMilliseconds,
                    _turnOutputAudioMilliseconds,
                    _toolRecords.ToList());

                _userText = string.Empty;
                _assistantText = string.Empty;
                _turnInputAudioMilliseconds = 0;
                _turnOutputAudioMilliseconds = 0;
                _toolRecords.Clear();
                return snapshot;
            }
        }
    }

    private sealed record VoiceTurnSnapshot(
        string UserText,
        string AssistantText,
        int InputAudioMilliseconds,
        int OutputAudioMilliseconds,
        IReadOnlyList<AiToolCallRecord> ToolRecords)
    {
        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(UserText)
            && string.IsNullOrWhiteSpace(AssistantText)
            && ToolRecords.Count == 0
            && InputAudioMilliseconds == 0
            && OutputAudioMilliseconds == 0;
    }

    private enum VoiceInboundKind
    {
        Audio,
        AudioEnd,
        Text
    }

    private sealed record VoiceInboundFrame(VoiceInboundKind Kind, byte[]? Audio = null, string? Text = null)
    {
        public static VoiceInboundFrame FromAudio(byte[] audio) => new(VoiceInboundKind.Audio, audio);
        public static VoiceInboundFrame FromAudioEnd() => new(VoiceInboundKind.AudioEnd);
        public static VoiceInboundFrame FromText(string text) => new(VoiceInboundKind.Text, Text: text);
    }

    private sealed record VoiceOutboundFrame(byte[]? Audio, object? Json)
    {
        public static VoiceOutboundFrame FromAudio(byte[] audio) => new(audio, null);
        public static VoiceOutboundFrame FromJson(object payload) => new(null, payload);
    }
}
