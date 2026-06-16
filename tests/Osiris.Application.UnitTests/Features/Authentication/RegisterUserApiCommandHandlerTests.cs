using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.Commands.RegisterUserApi;
using Osiris.Application.Features.Categories.Services;
using Osiris.Application.UnitTests.Features.Authentication.Support;
using Osiris.Application.UnitTests.Features.Categories.Support;

namespace Osiris.Application.UnitTests.Features.Authentication;

public sealed class RegisterUserApiCommandHandlerTests
{
    private static readonly DateTime Expiry = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    private static RegisterUserApiCommandHandler CreateHandler(
        FakeAuthIdentityService identityService,
        FakeCategoryRepository categories,
        FakeRefreshTokenRepository refreshTokens)
    {
        return new RegisterUserApiCommandHandler(
            identityService,
            new DefaultFinancialCategoriesSeeder(categories),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenFactory(Expiry),
            refreshTokens);
    }

    private static RegisterUserApiCommand Command()
    {
        return new RegisterUserApiCommand("Acme Finance", "Jane Owner", "jane@osiris.test", "password1", "password1");
    }

    [Fact]
    public async Task Handle_WhenRegistrationSucceeds_ShouldSeedCategoriesAndIssueTokens()
    {
        var tenantId = Guid.NewGuid();
        var identityService = new FakeAuthIdentityService
        {
            RegisterResult = Result<TenantRegistration>.Success(new TenantRegistration("user-1", tenantId))
        };
        var categories = new FakeCategoryRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var handler = CreateHandler(identityService, categories, refreshTokens);

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Finance", result.Value!.User.TenantName);
        Assert.Equal("Jane Owner", result.Value.User.FullName);
        Assert.Equal("jane@osiris.test", result.Value.User.Email);
        Assert.Single(refreshTokens.Tokens);
        Assert.NotEmpty(await categories.ListAsync(tenantId, includeArchived: true, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenRegistrationFails_ShouldNotSeedNorIssueTokens()
    {
        var identityService = new FakeAuthIdentityService
        {
            RegisterResult = Result<TenantRegistration>.Failure(
                new ResultError("Este e-mail já está cadastrado.", "Email"))
        };
        var categories = new FakeCategoryRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var handler = CreateHandler(identityService, categories, refreshTokens);

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(refreshTokens.Tokens);
        Assert.Empty(categories.Categories);
    }
}
