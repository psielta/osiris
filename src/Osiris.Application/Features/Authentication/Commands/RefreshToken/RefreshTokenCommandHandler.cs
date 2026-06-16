using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.DTOs;

namespace Osiris.Application.Features.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokensDto>>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenFactory _refreshTokenFactory;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenCommandHandler(
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenFactory refreshTokenFactory,
        IRefreshTokenRepository refreshTokens,
        IDateTimeProvider dateTimeProvider)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenFactory = refreshTokenFactory;
        _refreshTokens = refreshTokens;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AuthTokensDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var hash = _refreshTokenFactory.Hash(request.RefreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, cancellationToken);

        if (stored is null || !stored.IsActive(now))
        {
            // Presenting a token that exists but is already revoked means it was rotated and replayed —
            // treat it as theft and drop every active session for the user.
            if (stored is not null && stored.RevokedAtUtc is not null)
            {
                await _refreshTokens.RevokeAllForUserAsync(stored.UserId, now, cancellationToken);
            }

            return InvalidSession();
        }

        var profileResult = await _identityService.GetProfileAsync(stored.UserId, cancellationToken);
        if (profileResult.IsFailure || profileResult.Value is null)
        {
            return InvalidSession();
        }

        var profile = profileResult.Value;
        var replacement = _refreshTokenFactory.Create(stored.UserId, stored.TenantId);
        stored.Revoke(now, replacement.Token.Id);

        var rotated = await _refreshTokens.TryRotateAsync(stored, replacement.Token, cancellationToken);
        if (!rotated)
        {
            // A concurrent refresh already consumed this token. This is a benign race (not necessarily
            // theft), so just reject the loser — the winner's new token stays valid. Real theft is still
            // caught by the revoked-token-reuse branch above on the next attempt with the old token.
            return InvalidSession();
        }

        var accessToken = _jwtTokenGenerator.Generate(profile.UserId, profile.TenantId, profile.Email);

        return Result<AuthTokensDto>.Success(new AuthTokensDto(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            replacement.RawToken,
            replacement.Token.ExpiresAtUtc,
            "Bearer",
            new AuthUserDto(profile.FullName, profile.Email, profile.TenantName)));
    }

    private static Result<AuthTokensDto> InvalidSession() =>
        Result<AuthTokensDto>.Failure(
            new ResultError("Sessão inválida ou expirada.", null, ResultErrorCodes.InvalidRefreshToken));
}
