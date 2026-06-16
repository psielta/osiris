using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Api.Contracts;
using Osiris.Application.Features.Authentication.Commands.AuthenticateUser;
using Osiris.Application.Features.Authentication.Commands.RefreshToken;
using Osiris.Application.Features.Authentication.Commands.RegisterUserApi;
using Osiris.Application.Features.Authentication.Commands.RevokeRefreshToken;
using Osiris.Application.Features.Authentication.Queries.GetCurrentUser;

namespace Osiris.Api.Controllers.V1;

[Route("api/v1/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RegisterUserApiCommand(
                request.TenantName,
                request.FullName,
                request.Email,
                request.Password,
                request.ConfirmPassword),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : Problem(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AuthenticateUserCommand(request.Email, request.Password),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeRefreshTokenCommand(request.RefreshToken), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }
}
