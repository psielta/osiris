using System.Collections.Concurrent;

namespace Osiris.Application.Features.AiAssistant.Services;

public interface IAiVoiceSessionLimiter
{
    bool TryAcquire(Guid tenantId, string userId, int maxSessions, out IDisposable? lease);
}

public sealed class AiVoiceSessionLimiter : IAiVoiceSessionLimiter
{
    private readonly ConcurrentDictionary<string, int> _activeSessions = new();
    private readonly object _gate = new();

    public bool TryAcquire(Guid tenantId, string userId, int maxSessions, out IDisposable? lease)
    {
        lease = null;
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var limit = Math.Max(1, maxSessions);
        var key = $"{tenantId:N}:{userId}";

        lock (_gate)
        {
            var current = _activeSessions.GetValueOrDefault(key);
            if (current >= limit)
            {
                return false;
            }

            _activeSessions[key] = current + 1;
            lease = new Lease(this, key);
            return true;
        }
    }

    private void Release(string key)
    {
        lock (_gate)
        {
            if (!_activeSessions.TryGetValue(key, out var current))
            {
                return;
            }

            if (current <= 1)
            {
                _activeSessions.TryRemove(key, out _);
            }
            else
            {
                _activeSessions[key] = current - 1;
            }
        }
    }

    private sealed class Lease : IDisposable
    {
        private readonly AiVoiceSessionLimiter _owner;
        private readonly string _key;
        private int _disposed;

        public Lease(AiVoiceSessionLimiter owner, string key)
        {
            _owner = owner;
            _key = key;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _owner.Release(_key);
            }
        }
    }
}
