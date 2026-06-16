using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.Commands.AuthenticateUser;
using Osiris.Application.UnitTests.Features.Authentication.Support;

namespace Osiris.Application.UnitTests.Features.Authentication;

public sealed class AuthenticateUserCommandHandlerTests
{
    private static readonly DateTime Expiry = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    private static AuthenticateUserCommandHandler CreateHandler(
        FakeAuthIdentityService identityService,
        FakeRefreshTokenRepository refreshTokens)
    {
        return new AuthenticateUserCommandHandler(
            identityService,
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenFactory(Expiry),
            refreshTokens);
    }

    [Fact]
    public async Task Handle_WhenCredentialsValid_ShouldIssueTokensWithEmbeddedUserAndPersistRefresh()
    {
        var tenantId = Guid.NewGuid();
        var identityService = new FakeAuthIdentityService
        {
            CheckCredentialsResult = Result<UserProfileDto>.Success(
                new UserProfileDto("user-1", "jane@osiris.test", "Jane Owner", tenantId, "Acme Finance"))
        };
        var refreshTokens = new FakeRefreshTokenRepository();
        var handler = CreateHandler(identityService, refreshTokens);

        var result = await handler.Handle(
            new AuthenticateUserCommand("jane@osiris.test", "password1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Bearer", result.Value!.TokenType);
        Assert.False(string.IsNullOrEmpty(result.Value.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.Value.RefreshToken));
        Assert.Equal(Expiry, result.Value.RefreshTokenExpiresAtUtc);
        Assert.Equal("Jane Owner", result.Value.User.FullName);
        Assert.Equal("Acme Finance", result.Value.User.TenantName);
        Assert.Single(refreshTokens.Tokens);
    }

    [Fact]
    public async Task Handle_WhenCredentialsInvalid_ShouldFailAndPersistNothing()
    {
        var refreshTokens = new FakeRefreshTokenRepository();
        var handler = CreateHandler(new FakeAuthIdentityService(), refreshTokens);

        var result = await handler.Handle(
            new AuthenticateUserCommand("jane@osiris.test", "wrong"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(refreshTokens.Tokens);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.Unauthorized);
    }
}
