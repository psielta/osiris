namespace Osiris.Infrastructure.Identity;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section. Only the refresh-token lifetime is read here;
/// the rest of the JWT settings live in the API host.
/// </summary>
public sealed class RefreshTokenOptions
{
    public int RefreshTokenDays { get; set; } = 30;
}
