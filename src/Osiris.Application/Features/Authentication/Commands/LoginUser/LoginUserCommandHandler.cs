using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Commands.LoginUser;

public sealed class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result>
{
    private readonly IIdentityService _identityService;

    public LoginUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<Result> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        return _identityService.PasswordSignInAsync(
            request.Email,
            request.Password,
            request.RememberMe,
            cancellationToken);
    }
}
