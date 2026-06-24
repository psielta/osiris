using System.Collections.Concurrent;

namespace Osiris.Application.Features.AiAssistant.Services;

public interface IAiVoiceResumptionStore
{
    string? Get(Guid tenantId, string userId, Guid conversationId, DateTime utcNow);

    void Save(Guid tenantId, string userId, Guid conversationId, string handle, DateTime expiresAtUtc);

    void Clear(Guid tenantId, string userId, Guid conversationId);
}

public sealed class AiVoiceResumptionStore : IAiVoiceResumptionStore
{
    private readonly ConcurrentDictionary<string, Entry> _handles = new();

    public string? Get(Guid tenantId, string userId, Guid conversationId, DateTime utcNow)
    {
        var key = Key(tenantId, userId, conversationId);
        if (!_handles.TryGetValue(key, out var entry))
        {
            return null;
        }

        if (entry.ExpiresAtUtc <= utcNow)
        {
            _handles.TryRemove(key, out _);
            return null;
        }

        return entry.Handle;
    }

    public void Save(Guid tenantId, string userId, Guid conversationId, string handle, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(handle))
        {
            return;
        }

        _handles[Key(tenantId, userId, conversationId)] = new Entry(handle, expiresAtUtc);
    }

    public void Clear(Guid tenantId, string userId, Guid conversationId) =>
        _handles.TryRemove(Key(tenantId, userId, conversationId), out _);

    private static string Key(Guid tenantId, string userId, Guid conversationId) =>
        $"{tenantId:N}:{userId}:{conversationId:N}";

    private sealed record Entry(string Handle, DateTime ExpiresAtUtc);
}
