using Osiris.Application.Common.Models;

namespace Osiris.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<TenantRegistration>> RegisterTenantAndUserAsync(
        string tenantName,
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<Result> PasswordSignInAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates credentials (honoring lockout) WITHOUT establishing a cookie session, for token-based
    /// flows. Returns the user's profile on success.
    /// </summary>
    Task<Result<UserProfileDto>> CheckCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<Result<UserProfileDto>> GetProfileAsync(string userId, CancellationToken cancellationToken);

    Task<Result> SignInAsync(string userId, CancellationToken cancellationToken);

    Task<Result> SignOutAsync(CancellationToken cancellationToken);

    Task<Result<string?>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken);
}
