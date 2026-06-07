using Osiris.Application.Common.Models;

namespace Osiris.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<string>> RegisterTenantAndUserAsync(
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

    Task<Result> SignInAsync(string userId, CancellationToken cancellationToken);

    Task<Result> SignOutAsync(CancellationToken cancellationToken);

    Task<Result<string?>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken);
}
