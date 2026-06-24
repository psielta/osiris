using System.Diagnostics.Metrics;

namespace Osiris.Application.Features.AiAssistant.Services;

public sealed class AiVoiceTelemetry
{
    private readonly Meter _meter = new("Osiris.Ai.Voice", "1.0.0");
    private readonly Counter<long> _sessionsStarted;
    private readonly Counter<long> _sessionsRejected;
    private readonly Counter<long> _audioInputMilliseconds;
    private readonly Counter<long> _audioOutputMilliseconds;
    private readonly Counter<long> _framesDropped;
    private readonly Histogram<double> _sessionDurationMilliseconds;
    private readonly Histogram<double> _toolLatencyMilliseconds;

    public AiVoiceTelemetry()
    {
        _sessionsStarted = _meter.CreateCounter<long>("osiris.ai.voice.sessions.started");
        _sessionsRejected = _meter.CreateCounter<long>("osiris.ai.voice.sessions.rejected");
        _audioInputMilliseconds = _meter.CreateCounter<long>("osiris.ai.voice.audio.input.ms");
        _audioOutputMilliseconds = _meter.CreateCounter<long>("osiris.ai.voice.audio.output.ms");
        _framesDropped = _meter.CreateCounter<long>("osiris.ai.voice.frames.dropped");
        _sessionDurationMilliseconds = _meter.CreateHistogram<double>("osiris.ai.voice.session.duration.ms");
        _toolLatencyMilliseconds = _meter.CreateHistogram<double>("osiris.ai.voice.tool.latency.ms");
    }

    public void SessionStarted(Guid tenantId) =>
        _sessionsStarted.Add(1, TenantTag(tenantId));

    public void SessionRejected(Guid tenantId, string reason) =>
        _sessionsRejected.Add(1, TenantTag(tenantId), new KeyValuePair<string, object?>("reason", reason));

    public void AudioInput(Guid tenantId, int milliseconds)
    {
        if (milliseconds > 0)
        {
            _audioInputMilliseconds.Add(milliseconds, TenantTag(tenantId));
        }
    }

    public void AudioOutput(Guid tenantId, int milliseconds)
    {
        if (milliseconds > 0)
        {
            _audioOutputMilliseconds.Add(milliseconds, TenantTag(tenantId));
        }
    }

    public void FrameDropped(Guid tenantId, string direction) =>
        _framesDropped.Add(1, TenantTag(tenantId), new KeyValuePair<string, object?>("direction", direction));

    public void SessionDuration(Guid tenantId, TimeSpan duration) =>
        _sessionDurationMilliseconds.Record(duration.TotalMilliseconds, TenantTag(tenantId));

    public void ToolLatency(Guid tenantId, string toolName, int durationMs) =>
        _toolLatencyMilliseconds.Record(
            durationMs,
            TenantTag(tenantId),
            new KeyValuePair<string, object?>("tool", toolName));

    private static KeyValuePair<string, object?> TenantTag(Guid tenantId) =>
        new("tenant_id", tenantId.ToString("N"));
}
