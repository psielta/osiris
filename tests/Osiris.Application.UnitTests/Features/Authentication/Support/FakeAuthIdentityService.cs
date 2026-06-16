using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.UnitTests.Features.Authentication.Support;

/// <summary>
/// Hand-written <see cref="IIdentityService"/> double for the token-based auth handler tests.
/// Only the methods those handlers use are configurable; the cookie methods throw.
/// </summary>
internal sealed class FakeAuthIdentityService : IIdentityService
{
    public Result<UserProfileDto> CheckCredentialsResult { get; set; } =
        Result<UserProfileDto>.Failure(new ResultError("E-mail ou senha inválidos.", null, ResultErrorCodes.Unauthorized));

    public Result<UserProfileDto>? GetProfileResult { get; set; }

    public Result<TenantRegistration> RegisterResult { get; set; } =
        Result<TenantRegistration>.Success(new TenantRegistration("user-1", Guid.NewGuid()));

    public Task<Result<UserProfileDto>> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        return Task.FromResult(CheckCredentialsResult);
    }

    public Task<Result<UserProfileDto>> GetProfileAsync(string userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetProfileResult
            ?? Result<UserProfileDto>.Failure(new ResultError("Usuário não encontrado.", null, ResultErrorCodes.NotFound)));
    }

    public Task<Result<TenantRegistration>> RegisterTenantAndUserAsync(
        string tenantName,
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(RegisterResult);
    }

    public Task<Result> PasswordSignInAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<Result> SignInAsync(string userId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<Result> SignOutAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<Result<string?>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
