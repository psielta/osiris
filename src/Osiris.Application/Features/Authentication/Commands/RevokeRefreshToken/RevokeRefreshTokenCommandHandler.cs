using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Commands.RevokeRefreshToken;

public sealed class RevokeRefreshTokenCommandHandler : IRequestHandler<RevokeRefreshTokenCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IRefreshTokenFactory _refreshTokenFactory;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RevokeRefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IRefreshTokenFactory refreshTokenFactory,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _refreshTokens = refreshTokens;
        _refreshTokenFactory = refreshTokenFactory;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = _refreshTokenFactory.Hash(request.RefreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, cancellationToken);

        if (stored is not null
            && stored.RevokedAtUtc is null
            && string.Equals(stored.UserId, _currentUser.UserId, StringComparison.Ordinal))
        {
            stored.Revoke(_dateTimeProvider.UtcNow);
            await _refreshTokens.UpdateAsync(stored, cancellationToken);
        }

        // Logout is idempotent and must not reveal whether the token existed.
        return Result.Success();
    }
}
