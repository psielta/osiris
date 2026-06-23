using System.Globalization;
using System.Text.Json;
using Osiris.Application.Common.AI;

namespace Osiris.Infrastructure.AI.Gemini;

/// <summary>
/// Pure translation of a Gemini Live <c>BidiGenerateContent</c> server message into provider-neutral
/// <see cref="AiLiveServerEvent"/>s. Kept side-effect-free so it can be unit-tested with fixtures — the
/// wire format is the riskiest part of the integration.
/// </summary>
public static class GeminiLiveMessageParser
{
    private static readonly JsonElement EmptyArgs = JsonDocument.Parse("{}").RootElement.Clone();

    public static IReadOnlyList<AiLiveServerEvent> Parse(JsonElement root)
    {
        var events = new List<AiLiveServerEvent>();

        if (root.TryGetProperty("serverContent", out var serverContent))
        {
            ParseServerContent(serverContent, events);
        }

        if (root.TryGetProperty("toolCall", out var toolCall))
        {
            ParseToolCall(toolCall, events);
        }

        if (root.TryGetProperty("goAway", out var goAway))
        {
            events.Add(new AiLiveGoAway(ParseMillisLeft(goAway)));
        }

        return events;
    }

    private static void ParseServerContent(JsonElement serverContent, List<AiLiveServerEvent> events)
    {
        if (serverContent.TryGetProperty("modelTurn", out var modelTurn)
            && modelTurn.TryGetProperty("parts", out var parts)
            && parts.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in parts.EnumerateArray())
            {
                // Only audio. In AUDIO response mode the model's text parts are its (often English) thinking
                // trace, not the answer — the spoken answer is transcribed separately via outputTranscription.
                if (part.TryGetProperty("inlineData", out var inlineData)
                    && inlineData.TryGetProperty("data", out var dataEl)
                    && dataEl.ValueKind == JsonValueKind.String
                    && dataEl.GetString() is { Length: > 0 } base64)
                {
                    events.Add(new AiLiveAudioChunk(Convert.FromBase64String(base64)));
                }
            }
        }

        AddTranscript(serverContent, "outputTranscription", isUser: false, events);
        AddTranscript(serverContent, "inputTranscription", isUser: true, events);

        if (IsTrue(serverContent, "interrupted"))
        {
            events.Add(new AiLiveInterrupted());
        }

        if (IsTrue(serverContent, "turnComplete"))
        {
            events.Add(new AiLiveTurnComplete());
        }
    }

    private static void ParseToolCall(JsonElement toolCall, List<AiLiveServerEvent> events)
    {
        if (!toolCall.TryGetProperty("functionCalls", out var functionCalls)
            || functionCalls.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var calls = new List<AiModelToolCall>();
        foreach (var call in functionCalls.EnumerateArray())
        {
            var name = call.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var id = call.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
            var args = call.TryGetProperty("args", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object
                ? argsEl.Clone()
                : EmptyArgs;

            calls.Add(new AiModelToolCall(id, name, args));
        }

        if (calls.Count > 0)
        {
            events.Add(new AiLiveToolCallEvent(calls));
        }
    }

    private static void AddTranscript(JsonElement parent, string property, bool isUser, List<AiLiveServerEvent> events)
    {
        if (parent.TryGetProperty(property, out var node)
            && node.TryGetProperty("text", out var textEl)
            && textEl.ValueKind == JsonValueKind.String
            && textEl.GetString() is { Length: > 0 } text)
        {
            events.Add(new AiLiveTranscript(text, isUser, Final: false));
        }
    }

    private static bool IsTrue(JsonElement parent, string property) =>
        parent.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>Reads <c>goAway.timeLeft</c>, accepting a protobuf duration string ("10s") or a number.</summary>
    private static int ParseMillisLeft(JsonElement goAway)
    {
        if (!goAway.TryGetProperty("timeLeft", out var timeLeft))
        {
            return 0;
        }

        if (timeLeft.ValueKind == JsonValueKind.Number && timeLeft.TryGetInt32(out var ms))
        {
            return ms;
        }

        if (timeLeft.ValueKind == JsonValueKind.String && timeLeft.GetString() is { } raw)
        {
            var trimmed = raw.TrimEnd('s', 'S');
            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            {
                return (int)(seconds * 1000);
            }
        }

        return 0;
    }
}
