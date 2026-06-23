using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Interfaces;

namespace Osiris.Web.Controllers;

/// <summary>
/// Cookie-authenticated realtime voice endpoint. The browser opens a WebSocket to <c>/assistant/voice</c>;
/// this controller proxies it to a Gemini Live session (key stays server-side), relays audio both ways and
/// executes tool calls through the shared <see cref="IAiLiveToolDispatcher"/>. Gated by the AiAssistant +
/// AiAssistantVoice flags (404 when off). Read-only for now (Fase 1): writes are disabled in the context.
/// </summary>
[Authorize]
[Route("assistant")]
public sealed class AiVoiceController : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly AiFeatureOptions _features;
    private readonly IAiLiveSessionClient _liveClient;
    private readonly IAiLiveToolDispatcher _dispatcher;
    private readonly IAiPromptBuilder _promptBuilder;
    private readonly IAiToolRegistry _toolRegistry;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AiVoiceController> _logger;

    public AiVoiceController(
        IOptions<AiFeatureOptions> features,
        IAiLiveSessionClient liveClient,
        IAiLiveToolDispatcher dispatcher,
        IAiPromptBuilder promptBuilder,
        IAiToolRegistry toolRegistry,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ILogger<AiVoiceController> logger)
    {
        _features = features.Value;
        _liveClient = liveClient;
        _dispatcher = dispatcher;
        _promptBuilder = promptBuilder;
        _toolRegistry = toolRegistry;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    [HttpGet("voice")]
    public async Task<IActionResult> Voice()
    {
        if (!_features.AiAssistant || !_features.AiAssistantVoice)
        {
            return NotFound();
        }

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            return BadRequest("Esperado um WebSocket.");
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var cancellation = HttpContext.RequestAborted;

        var context = new AiAgentContext(
            _currentUser.TenantId,
            _currentUser.UserId ?? string.Empty,
            Guid.NewGuid(),
            Guid.NewGuid().ToString("n"),
            DateOnly.FromDateTime(_clock.UtcNow),
            WritesEnabled: false);

        var prompt = _promptBuilder.BuildSystemPrompt(context);
        var tools = _toolRegistry.GetAllowedTools(context)
            .Select(tool => new AiToolDefinition(tool.Name, tool.Description, tool.InputSchema))
            .ToList();

        try
        {
            await using var session = await _liveClient.ConnectAsync(
                new AiLiveSessionRequest(prompt.SystemPrompt, tools, new AiLiveAudioConfig(), context.CorrelationId),
                cancellation);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var clientPump = PumpClientToGeminiAsync(socket, session, cts.Token);
            var serverPump = PumpGeminiToClientAsync(socket, session, context, cts.Token);

            await Task.WhenAny(clientPump, serverPump);
            cts.Cancel();
            await Task.WhenAll(Swallow(clientPump), Swallow(serverPump));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Voice session failed.");
            await TrySendJsonAsync(socket, new { type = "error", message = "Sessão de voz indisponível." }, cancellation);
        }
        finally
        {
            await CloseAsync(socket);
        }

        return new EmptyResult();
    }

    private async Task PumpClientToGeminiAsync(WebSocket socket, IAiLiveSession session, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var message = new MemoryStream();

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

                message.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Binary)
            {
                await session.SendAudioAsync(message.GetBuffer().AsMemory(0, (int)message.Length), cancellationToken);
            }
            else if (!await HandleControlAsync(session, message, cancellationToken))
            {
                return; // client asked to stop
            }
        }
    }

    private async Task PumpGeminiToClientAsync(
        WebSocket socket, IAiLiveSession session, AiAgentContext context, CancellationToken cancellationToken)
    {
        await foreach (var serverEvent in session.ReadEventsAsync(cancellationToken))
        {
            switch (serverEvent)
            {
                case AiLiveAudioChunk audio:
                    await socket.SendAsync(audio.Pcm24, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);
                    break;

                case AiLiveTranscript transcript:
                    await TrySendJsonAsync(socket, new
                    {
                        type = "transcript",
                        role = transcript.IsUser ? "user" : "assistant",
                        text = transcript.Text,
                        final = transcript.Final
                    }, cancellationToken);
                    break;

                case AiLiveToolCallEvent toolCall:
                    var batch = await _dispatcher.DispatchAsync(context, toolCall.Calls, cancellationToken);
                    await session.SendToolResultsAsync(batch.Results, cancellationToken);
                    if (batch.Sources.Count > 0)
                    {
                        await TrySendJsonAsync(socket, new { type = "sources", items = batch.Sources }, cancellationToken);
                    }

                    break;

                case AiLiveInterrupted:
                    await TrySendJsonAsync(socket, new { type = "status", value = "interrupted" }, cancellationToken);
                    break;

                case AiLiveTurnComplete:
                    await TrySendJsonAsync(socket, new { type = "status", value = "idle" }, cancellationToken);
                    break;

                case AiLiveGoAway goAway:
                    await TrySendJsonAsync(socket, new { type = "status", value = "goingaway", millisLeft = goAway.MillisLeft }, cancellationToken);
                    break;
            }
        }
    }

    private static async Task<bool> HandleControlAsync(IAiLiveSession session, MemoryStream message, CancellationToken cancellationToken)
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
                await session.SignalAudioEndAsync(cancellationToken);
                return true;
            case "text" when !string.IsNullOrWhiteSpace(content):
                await session.SendTextAsync(content!, cancellationToken);
                return true;
            default:
                return true;
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
            // Client went away; the pumps will unwind.
        }
    }

    private static async Task Swallow(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception exception) when (exception is OperationCanceledException or WebSocketException or ObjectDisposedException)
        {
            // Expected on teardown.
        }
    }

    private static async Task CloseAsync(WebSocket socket)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }
            catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException or OperationCanceledException)
            {
                // Best-effort.
            }
        }
    }
}
