using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Exceptions;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Gemini;

namespace Osiris.Infrastructure.AI.Gemini;

/// <summary>
/// <see cref="IAiLiveSessionClient"/> over the Gemini Live <c>BidiGenerateContent</c> WebSocket. This is
/// the only place that knows the Live wire format; the relay/orchestration stays provider-neutral. The
/// API key stays server-side (proxy mode) — the browser/mobile never see it.
/// </summary>
public sealed class GeminiLiveSessionClient : IAiLiveSessionClient
{
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiLiveSessionClient> _logger;

    public GeminiLiveSessionClient(IOptions<GeminiOptions> options, ILogger<GeminiLiveSessionClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IAiLiveSession> ConnectAsync(AiLiveSessionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new AiModelException("O assistente de voz não está configurado.");
        }

        var host = new Uri(_options.BaseUrl).Host;
        var url = $"wss://{host}/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent?key={_options.ApiKey}";

        var socket = new ClientWebSocket();
        try
        {
            await socket.ConnectAsync(new Uri(url), cancellationToken);
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            socket.Dispose();
            _logger.LogError(exception, "Gemini Live connect failed.");
            throw new AiModelException("O assistente de voz está temporariamente indisponível.", exception);
        }

        var session = new GeminiLiveSession(socket, _options.LiveModel, _options.LiveVoice, _logger);
        await session.SendSetupAsync(request, cancellationToken);
        return session;
    }
}

/// <summary>One live session. Sends are serialized with a lock because the relay writes from two pumps
/// (client audio in + tool responses out).</summary>
internal sealed class GeminiLiveSession : IAiLiveSession
{
    private const string InputMime = "audio/pcm;rate=16000";

    private readonly ClientWebSocket _socket;
    private readonly string _model;
    private readonly string? _voice;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public GeminiLiveSession(ClientWebSocket socket, string model, string? voice, ILogger logger)
    {
        _socket = socket;
        _model = model;
        _voice = voice;
        _logger = logger;
    }

    public async Task SendSetupAsync(AiLiveSessionRequest request, CancellationToken cancellationToken)
    {
        var generationConfig = new JsonObject
        {
            ["responseModalities"] = new JsonArray("AUDIO")
        };

        var voiceName = request.Audio.VoiceName ?? _voice;
        if (!string.IsNullOrWhiteSpace(voiceName))
        {
            generationConfig["speechConfig"] = new JsonObject
            {
                ["voiceConfig"] = new JsonObject
                {
                    ["prebuiltVoiceConfig"] = new JsonObject { ["voiceName"] = voiceName }
                }
            };
        }

        var setup = new JsonObject
        {
            ["model"] = _model.StartsWith("models/", StringComparison.Ordinal) ? _model : $"models/{_model}",
            ["generationConfig"] = generationConfig,
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = request.SystemPrompt })
            },
            ["sessionResumption"] = BuildSessionResumption(request.ResumptionHandle)
        };

        if (request.Tools.Count > 0)
        {
            var declarations = new JsonArray();
            foreach (var tool in request.Tools)
            {
                var declaration = new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = JsonSerializer.SerializeToNode(tool.ParametersSchema)
                };

                if (tool.Risk == AiToolRisk.ReadOnly)
                {
                    declaration["behavior"] = "NON_BLOCKING";
                }

                declarations.Add(declaration);
            }

            setup["tools"] = new JsonArray(new JsonObject { ["functionDeclarations"] = declarations });
        }

        if (request.Audio.OutputTranscription)
        {
            setup["outputAudioTranscription"] = new JsonObject();
        }

        if (request.Audio.InputTranscription)
        {
            setup["inputAudioTranscription"] = new JsonObject();
        }

        await SendAsync(new JsonObject { ["setup"] = setup }, cancellationToken);
    }

    public Task SendAudioAsync(ReadOnlyMemory<byte> pcm16, CancellationToken cancellationToken) =>
        SendAsync(new JsonObject
        {
            ["realtimeInput"] = new JsonObject
            {
                ["audio"] = new JsonObject
                {
                    ["mimeType"] = InputMime,
                    ["data"] = Convert.ToBase64String(pcm16.Span)
                }
            }
        }, cancellationToken);

    public Task SignalAudioEndAsync(CancellationToken cancellationToken) =>
        SendAsync(new JsonObject
        {
            ["realtimeInput"] = new JsonObject { ["audioStreamEnd"] = new JsonObject() }
        }, cancellationToken);

    public Task SendTextAsync(string text, CancellationToken cancellationToken) =>
        SendAsync(new JsonObject
        {
            ["clientContent"] = new JsonObject
            {
                ["turns"] = new JsonArray(new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray(new JsonObject { ["text"] = text })
                }),
                ["turnComplete"] = true
            }
        }, cancellationToken);

    public Task SendToolResultsAsync(IReadOnlyList<AiModelToolResult> results, CancellationToken cancellationToken)
    {
        var responses = new JsonArray();
        foreach (var result in results)
        {
            var node = new JsonObject
            {
                ["name"] = result.Name,
                ["response"] = new JsonObject { ["result"] = ParseOrString(result.ResultJson) }
            };
            if (!string.IsNullOrEmpty(result.Id))
            {
                node["id"] = result.Id;
            }

            responses.Add(node);
        }

        return SendAsync(new JsonObject
        {
            ["toolResponse"] = new JsonObject { ["functionResponses"] = responses }
        }, cancellationToken);
    }

    public async IAsyncEnumerable<AiLiveServerEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var message = new MemoryStream();

        while (!cancellationToken.IsCancellationRequested && _socket.State == WebSocketState.Open)
        {
            if (!await ReceiveFullMessageAsync(buffer, message, cancellationToken))
            {
                yield break;
            }

            if (message.Length == 0)
            {
                continue;
            }

            foreach (var serverEvent in ParseSafe(message))
            {
                yield return serverEvent;
            }
        }
    }

    private async Task<bool> ReceiveFullMessageAsync(byte[] buffer, MemoryStream message, CancellationToken cancellationToken)
    {
        message.SetLength(0);
        while (true)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            }
            catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or ObjectDisposedException)
            {
                return false;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return false;
            }

            message.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                return true;
            }
        }
    }

    private IReadOnlyList<AiLiveServerEvent> ParseSafe(MemoryStream message)
    {
        try
        {
            using var document = JsonDocument.Parse(message.GetBuffer().AsMemory(0, (int)message.Length));
            return GeminiLiveMessageParser.Parse(document.RootElement);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Gemini Live sent an unparsable message.");
            return Array.Empty<AiLiveServerEvent>();
        }
    }

    private async Task SendAsync(JsonObject message, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(message.ToJsonString());
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static JsonNode? ParseOrString(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static JsonObject BuildSessionResumption(string? handle)
    {
        var resumption = new JsonObject();
        if (!string.IsNullOrWhiteSpace(handle))
        {
            resumption["handle"] = handle;
        }

        return resumption;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }
        }
        catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException or OperationCanceledException)
        {
            // Best-effort close.
        }
        finally
        {
            _socket.Dispose();
            _sendLock.Dispose();
        }
    }
}
