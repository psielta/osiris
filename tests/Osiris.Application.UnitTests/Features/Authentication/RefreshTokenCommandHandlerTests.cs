using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.Commands.RefreshToken;
using Osiris.Application.UnitTests.Features.Authentication.Support;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.Authentication;

public sealed class RefreshTokenCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Expiry = Now.AddDays(30);

    // FakeRefreshTokenFactory.Hash("existing") == "hash:existing".
    private const string ExistingRaw = "existing";
    private const string ExistingHash = "hash:existing";

    private static RefreshTokenCommandHandler CreateHandler(
        FakeAuthIdentityService identityService,
        FakeRefreshTokenRepository refreshTokens)
    {
        return new RefreshTokenCommandHandler(
            identityService,
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenFactory(Expiry),
            refreshTokens,
            new FakeDateTimeProvider(Now));
    }

    private static FakeAuthIdentityService IdentityWithProfile(Guid tenantId)
    {
        return new FakeAuthIdentityService
        {
            GetProfileResult = Result<UserProfileDto>.Success(
                new UserProfileDto("user-1", "jane@osiris.test", "Jane", tenantId, "Acme"))
        };
    }

    [Fact]
    public async Task Handle_WhenTokenActive_ShouldRotateAndReturnNewTokens()
    {
        var tenantId = Guid.NewGuid();
        var refreshTokens = new FakeRefreshTokenRepository();
        var existing = new RefreshToken(ExistingHash, "user-1", tenantId, Expiry);
        await refreshTokens.AddAsync(existing, CancellationToken.None);
        var handler = CreateHandler(IdentityWithProfile(tenantId), refreshTokens);

        var result = await handler.Handle(new RefreshTokenCommand(ExistingRaw), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(existing.RevokedAtUtc);
        Assert.Equal(2, refreshTokens.Tokens.Count);
        Assert.Contains(refreshTokens.Tokens, token => token.RevokedAtUtc == null);
        Assert.Equal(0, refreshTokens.RevokeAllCallCount);
    }

    [Fact]
    public async Task Handle_WhenTokenAlreadyRevoked_ShouldFailAndRevokeAllSessions()
    {
        var refreshTokens = new FakeRefreshTokenRepository();
        var revoked = new RefreshToken(ExistingHash, "user-1", Guid.NewGuid(), Expiry);
        revoked.Revoke(Now);
        await refreshTokens.AddAsync(revoked, CancellationToken.None);
        var handler = CreateHandler(new FakeAuthIdentityService(), refreshTokens);

        var result = await handler.Handle(new RefreshTokenCommand(ExistingRaw), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.InvalidRefreshToken);
        Assert.Equal(1, refreshTokens.RevokeAllCallCount);
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ShouldFailWithoutRevokingAll()
    {
        var refreshTokens = new FakeRefreshTokenRepository();
        var expired = new RefreshToken(ExistingHash, "user-1", Guid.NewGuid(), Now.AddDays(-1));
        await refreshTokens.AddAsync(expired, CancellationToken.None);
        var handler = CreateHandler(new FakeAuthIdentityService(), refreshTokens);

        var result = await handler.Handle(new RefreshTokenCommand(ExistingRaw), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.InvalidRefreshToken);
        Assert.Equal(0, refreshTokens.RevokeAllCallCount);
    }

    [Fact]
    public async Task Handle_WhenRotationConflicts_ShouldFailWithoutRevokingAll()
    {
        var tenantId = Guid.NewGuid();
        var refreshTokens = new FakeRefreshTokenRepository { RotateShouldConflict = true };
        var existing = new RefreshToken(ExistingHash, "user-1", tenantId, Expiry);
        await refreshTokens.AddAsync(existing, CancellationToken.None);
        var handler = CreateHandler(IdentityWithProfile(tenantId), refreshTokens);

        var result = await handler.Handle(new RefreshTokenCommand(ExistingRaw), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.InvalidRefreshToken);
        Assert.Equal(0, refreshTokens.RevokeAllCallCount);
    }

    [Fact]
    public async Task Handle_WhenTokenUnknown_ShouldFail()
    {
        var refreshTokens = new FakeRefreshTokenRepository();
        var handler = CreateHandler(new FakeAuthIdentityService(), refreshTokens);

        var result = await handler.Handle(new RefreshTokenCommand("nope"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.InvalidRefreshToken);
        Assert.Equal(0, refreshTokens.RevokeAllCallCount);
    }
}
