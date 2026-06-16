using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.DTOs;

namespace Osiris.Application.Features.Authentication.Commands.AuthenticateUser;

public sealed class AuthenticateUserCommandHandler : IRequestHandler<AuthenticateUserCommand, Result<AuthTokensDto>>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenFactory _refreshTokenFactory;
    private readonly IRefreshTokenRepository _refreshTokens;

    public AuthenticateUserCommandHandler(
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenFactory refreshTokenFactory,
        IRefreshTokenRepository refreshTokens)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenFactory = refreshTokenFactory;
        _refreshTokens = refreshTokens;
    }

    public async Task<Result<AuthTokensDto>> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
    {
        var credentials = await _identityService.CheckCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (credentials.IsFailure || credentials.Value is null)
        {
            return Result<AuthTokensDto>.Failure(credentials.Errors);
        }

        var profile = credentials.Value;

        var accessToken = _jwtTokenGenerator.Generate(profile.UserId, profile.TenantId, profile.Email);
        var refresh = _refreshTokenFactory.Create(profile.UserId, profile.TenantId);
        await _refreshTokens.AddAsync(refresh.Token, cancellationToken);

        return Result<AuthTokensDto>.Success(new AuthTokensDto(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refresh.RawToken,
            refresh.Token.ExpiresAtUtc,
            "Bearer",
            new AuthUserDto(profile.FullName, profile.Email, profile.TenantName)));
    }
}
