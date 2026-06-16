using Osiris.Application.Common.Models;

namespace Osiris.Application.Common.Interfaces;

/// <summary>
/// Mints refresh tokens (cryptographically random raw value + stored hash, with the configured
/// lifetime) and hashes incoming raw tokens for lookup. Keeps the crypto out of the handlers.
/// </summary>
public interface IRefreshTokenFactory
{
    RefreshTokenCreation Create(string userId, Guid tenantId);

    string Hash(string rawToken);
}
