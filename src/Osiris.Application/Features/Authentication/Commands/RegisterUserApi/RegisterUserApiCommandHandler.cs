using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.DTOs;
using Osiris.Application.Features.Categories.Services;

namespace Osiris.Application.Features.Authentication.Commands.RegisterUserApi;

public sealed class RegisterUserApiCommandHandler : IRequestHandler<RegisterUserApiCommand, Result<AuthTokensDto>>
{
    private readonly IIdentityService _identityService;
    private readonly DefaultFinancialCategoriesSeeder _categoriesSeeder;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenFactory _refreshTokenFactory;
    private readonly IRefreshTokenRepository _refreshTokens;

    public RegisterUserApiCommandHandler(
        IIdentityService identityService,
        DefaultFinancialCategoriesSeeder categoriesSeeder,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenFactory refreshTokenFactory,
        IRefreshTokenRepository refreshTokens)
    {
        _identityService = identityService;
        _categoriesSeeder = categoriesSeeder;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenFactory = refreshTokenFactory;
        _refreshTokens = refreshTokens;
    }

    public async Task<Result<AuthTokensDto>> Handle(RegisterUserApiCommand request, CancellationToken cancellationToken)
    {
        var registration = await _identityService.RegisterTenantAndUserAsync(
            request.TenantName,
            request.FullName,
            request.Email,
            request.Password,
            cancellationToken);

        if (registration.IsFailure || registration.Value is null)
        {
            return Result<AuthTokensDto>.Failure(registration.Errors);
        }

        // Every new tenant starts with the default category set so the first screens are usable.
        await _categoriesSeeder.SeedAsync(registration.Value.TenantId, cancellationToken);

        var profile = new UserProfileDto(
            registration.Value.UserId,
            request.Email.Trim(),
            request.FullName.Trim(),
            registration.Value.TenantId,
            request.TenantName.Trim());

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
