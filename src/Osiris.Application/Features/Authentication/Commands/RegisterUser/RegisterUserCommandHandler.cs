using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Categories.Services;

namespace Osiris.Application.Features.Authentication.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly DefaultFinancialCategoriesSeeder _categoriesSeeder;

    public RegisterUserCommandHandler(
        IIdentityService identityService,
        DefaultFinancialCategoriesSeeder categoriesSeeder)
    {
        _identityService = identityService;
        _categoriesSeeder = categoriesSeeder;
    }

    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var registration = await _identityService.RegisterTenantAndUserAsync(
            request.TenantName,
            request.FullName,
            request.Email,
            request.Password,
            cancellationToken);

        if (registration.IsFailure || registration.Value is null)
        {
            return Result.Failure(registration.Errors);
        }

        // Every new tenant starts with the default category set so the first screens are usable.
        await _categoriesSeeder.SeedAsync(registration.Value.TenantId, cancellationToken);

        return await _identityService.SignInAsync(registration.Value.UserId, cancellationToken);
    }
}
