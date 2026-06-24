using System.Text.Json;
using Osiris.Application.Common.AI;
using Osiris.Infrastructure.AI.Gemini;

namespace Osiris.Web.IntegrationTests.AiAssistant;

// Pure parser tests — no web host / database, just fixtures of Gemini Live server messages.
public sealed class GeminiLiveMessageParserTests
{
    private static IReadOnlyList<AiLiveServerEvent> Parse(string json) =>
        GeminiLiveMessageParser.Parse(JsonDocument.Parse(json).RootElement);

    [Fact]
    public void Parses_audio_chunk_from_model_turn_inline_data()
    {
        var pcm = new byte[] { 1, 2, 3, 4 };
        var base64 = Convert.ToBase64String(pcm);
        var json = """
        {"serverContent":{"modelTurn":{"parts":[{"inlineData":{"mimeType":"audio/pcm;rate=24000","data":"__B64__"}}]}}}
        """.Replace("__B64__", base64);

        var chunk = Assert.IsType<AiLiveAudioChunk>(Assert.Single(Parse(json)));
        Assert.Equal(pcm, chunk.Pcm24.ToArray());
    }

    [Fact]
    public void Parses_tool_call_with_id_name_and_args()
    {
        var json = """
        {"toolCall":{"functionCalls":[{"id":"c1","name":"list_credit_cards","args":{"includeArchived":true}}]}}
        """;

        var toolCall = Assert.IsType<AiLiveToolCallEvent>(Assert.Single(Parse(json)));
        var call = Assert.Single(toolCall.Calls);
        Assert.Equal("c1", call.Id);
        Assert.Equal("list_credit_cards", call.Name);
        Assert.True(call.Arguments.GetProperty("includeArchived").GetBoolean());
    }

    [Fact]
    public void Parses_output_transcription_and_turn_complete()
    {
        var json = """
        {"serverContent":{"outputTranscription":{"text":"Olá"},"turnComplete":true}}
        """;

        var events = Parse(json);
        var transcript = Assert.IsType<AiLiveTranscript>(events[0]);
        Assert.False(transcript.IsUser);
        Assert.Equal("Olá", transcript.Text);
        Assert.IsType<AiLiveTurnComplete>(events[1]);
    }

    [Fact]
    public void Parses_interrupted_signal()
    {
        var interrupted = Parse("""{"serverContent":{"interrupted":true}}""");
        Assert.IsType<AiLiveInterrupted>(Assert.Single(interrupted));
    }

    [Fact]
    public void Parses_go_away_duration_string()
    {
        var goAway = Assert.IsType<AiLiveGoAway>(Assert.Single(Parse("""{"goAway":{"timeLeft":"5s"}}""")));
        Assert.Equal(5000, goAway.MillisLeft);
    }

    [Fact]
    public void Parses_session_resumption_update()
    {
        var update = Assert.IsType<AiLiveSessionResumptionUpdate>(
            Assert.Single(Parse("""{"sessionResumptionUpdate":{"newHandle":"resume-1","resumable":true}}""")));

        Assert.True(update.Resumable);
        Assert.Equal("resume-1", update.Handle);
    }

    [Fact]
    public void Ignores_setup_complete()
    {
        Assert.Empty(Parse("""{"setupComplete":{}}"""));
    }
}
