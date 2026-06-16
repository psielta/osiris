namespace Osiris.Application.Features.Authentication.DTOs;

/// <summary>
/// The token pair returned by login/register/refresh. <see cref="User"/> is embedded so the mobile
/// client can render the authenticated home without an extra round-trip to <c>/me</c>.
/// </summary>
public sealed record AuthTokensDto(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    string TokenType,
    AuthUserDto User);
