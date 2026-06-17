namespace Osiris.Api.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(
    string TenantName,
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword);

public sealed record ForgotPasswordRequest(string Email);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);
