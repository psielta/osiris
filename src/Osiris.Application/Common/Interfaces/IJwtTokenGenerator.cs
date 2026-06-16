namespace Osiris.Application.Common.Interfaces;

/// <summary>
/// Issues signed JWT access tokens. The implementation is provided by the API host (the only host
/// that mints JWTs); the access token carries the same claims the app relies on
/// (NameIdentifier, tenant_id, email) so <see cref="ICurrentUser"/> works unchanged.
/// </summary>
public interface IJwtTokenGenerator
{
    AccessToken Generate(string userId, Guid tenantId, string email);
}

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
