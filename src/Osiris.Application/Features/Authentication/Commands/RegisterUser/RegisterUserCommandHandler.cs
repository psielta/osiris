using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result>
{
    private readonly IIdentityService _identityService;

    public RegisterUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var registration = await _identityService.RegisterTenantAndUserAsync(
            request.TenantName,
            request.FullName,
            request.Email,
            request.Password,
            cancellationToken);

        if (registration.IsFailure || string.IsNullOrWhiteSpace(registration.Value))
        {
            return Result.Failure(registration.Errors);
        }

        return await _identityService.SignInAsync(registration.Value, cancellationToken);
    }
}
