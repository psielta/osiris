using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.Commands.RegisterUser;

namespace Osiris.Application.UnitTests.Features.Authentication;

public sealed class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRegistrationSucceeds_ShouldSignInCreatedUser()
    {
        var identityService = new FakeIdentityService
        {
            RegisterResult = Result<string>.Success("user-123"),
            SignInResult = Result.Success()
        };
        var handler = new RegisterUserCommandHandler(identityService);
        var command = new RegisterUserCommand(
            "Acme Finance",
            "Jane Owner",
            "jane@osiris.test",
            "password1",
            "password1");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Finance", identityService.RegisteredTenantName);
        Assert.Equal("Jane Owner", identityService.RegisteredFullName);
        Assert.Equal("jane@osiris.test", identityService.RegisteredEmail);
        Assert.Equal("password1", identityService.RegisteredPassword);
        Assert.Equal("user-123", identityService.SignedInUserId);
    }

    [Fact]
    public async Task Handle_WhenRegistrationFails_ShouldNotSignIn()
    {
        var expectedError = new ResultError("Email already exists.", nameof(RegisterUserCommand.Email));
        var identityService = new FakeIdentityService
        {
            RegisterResult = Result<string>.Failure(expectedError)
        };
        var handler = new RegisterUserCommandHandler(identityService);
        var command = new RegisterUserCommand(
            "Acme Finance",
            "Jane Owner",
            "jane@osiris.test",
            "password1",
            "password1");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(expectedError, result.Errors);
        Assert.Null(identityService.SignedInUserId);
    }

    private sealed class FakeIdentityService : IIdentityService
    {
        public Result<string> RegisterResult { get; init; } = Result<string>.Success("user-id");

        public Result SignInResult { get; init; } = Result.Success();

        public string? RegisteredTenantName { get; private set; }

        public string? RegisteredFullName { get; private set; }

        public string? RegisteredEmail { get; private set; }

        public string? RegisteredPassword { get; private set; }

        public string? SignedInUserId { get; private set; }

        public Task<Result<string>> RegisterTenantAndUserAsync(
            string tenantName,
            string fullName,
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            RegisteredTenantName = tenantName;
            RegisteredFullName = fullName;
            RegisteredEmail = email;
            RegisteredPassword = password;

            return Task.FromResult(RegisterResult);
        }

        public Task<Result> PasswordSignInAsync(
            string email,
            string password,
            bool rememberMe,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result> SignInAsync(string userId, CancellationToken cancellationToken)
        {
            SignedInUserId = userId;
            return Task.FromResult(SignInResult);
        }

        public Task<Result> SignOutAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<string?>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
