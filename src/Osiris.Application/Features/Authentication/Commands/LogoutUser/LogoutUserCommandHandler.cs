using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Commands.LogoutUser;

public sealed class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, Result>
{
    private readonly IIdentityService _identityService;

    public LogoutUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<Result> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        return _identityService.SignOutAsync(cancellationToken);
    }
}
