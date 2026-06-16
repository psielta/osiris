using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically revokes <paramref name="current"/> and inserts <paramref name="replacement"/> in a
    /// single transaction. Returns <c>false</c> when an optimistic-concurrency conflict means another
    /// rotation already consumed the token (treated by callers as reuse).
    /// </summary>
    Task<bool> TryRotateAsync(RefreshToken current, RefreshToken replacement, CancellationToken cancellationToken);

    Task RevokeAllForUserAsync(string userId, DateTime utcNow, CancellationToken cancellationToken);
}
