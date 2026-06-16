using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.Commands.RegisterUser;
using Osiris.Application.Features.Categories.Services;
using Osiris.Application.UnitTests.Features.Categories.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Authentication;

public sealed class RegisterUserCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly FakeCategoryRepository _categories = new();

    private RegisterUserCommandHandler CreateHandler(FakeIdentityService identityService)
    {
        return new RegisterUserCommandHandler(
            identityService,
            new DefaultFinancialCategoriesSeeder(_categories));
    }

    private static RegisterUserCommand Command()
    {
        return new RegisterUserCommand(
            "Acme Finance",
            "Jane Owner",
            "jane@osiris.test",
            "password1",
            "password1");
    }

    [Fact]
    public async Task Handle_WhenRegistrationSucceeds_ShouldSignInCreatedUser()
    {
        var identityService = new FakeIdentityService
        {
            RegisterResult = Result<TenantRegistration>.Success(new TenantRegistration("user-123", TenantId)),
            SignInResult = Result.Success()
        };
        var handler = CreateHandler(identityService);

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Finance", identityService.RegisteredTenantName);
        Assert.Equal("Jane Owner", identityService.RegisteredFullName);
        Assert.Equal("jane@osiris.test", identityService.RegisteredEmail);
        Assert.Equal("password1", identityService.RegisteredPassword);
        Assert.Equal("user-123", identityService.SignedInUserId);
    }

    [Fact]
    public async Task Handle_WhenRegistrationSucceeds_ShouldSeedDefaultCategoriesForTenant()
    {
        var identityService = new FakeIdentityService
        {
            RegisterResult = Result<TenantRegistration>.Success(new TenantRegistration("user-123", TenantId))
        };
        var handler = CreateHandler(identityService);

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var categories = await _categories.ListAsync(TenantId, includeArchived: true, CancellationToken.None);
        Assert.NotEmpty(categories);
        Assert.All(categories, category => Assert.Equal(TenantId, category.TenantId));
        Assert.Contains(categories, category =>
            category.Name == "Salário" && category.Type == CategoryType.Income);
        Assert.Contains(categories, category =>
            category.Name == "Moradia" && category.Type == CategoryType.Expense);
    }

    [Fact]
    public async Task Handle_WhenRegistrationFails_ShouldNotSignInNorSeed()
    {
        var expectedError = new ResultError("Email already exists.", nameof(RegisterUserCommand.Email));
        var identityService = new FakeIdentityService
        {
            RegisterResult = Result<TenantRegistration>.Failure(expectedError)
        };
        var handler = CreateHandler(identityService);

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(expectedError, result.Errors);
        Assert.Null(identityService.SignedInUserId);
        Assert.Empty(await _categories.ListAsync(TenantId, includeArchived: true, CancellationToken.None));
    }

    private sealed class FakeIdentityService : IIdentityService
    {
        public Result<TenantRegistration> RegisterResult { get; init; } =
            Result<TenantRegistration>.Success(new TenantRegistration("user-id", Guid.NewGuid()));

        public Result SignInResult { get; init; } = Result.Success();

        public string? RegisteredTenantName { get; private set; }

        public string? RegisteredFullName { get; private set; }

        public string? RegisteredEmail { get; private set; }

        public string? RegisteredPassword { get; private set; }

        public string? SignedInUserId { get; private set; }

        public Task<Result<TenantRegistration>> RegisterTenantAndUserAsync(
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

        public Task<Result<UserProfileDto>> CheckCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<UserProfileDto>> GetProfileAsync(string userId, CancellationToken cancellationToken)
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
