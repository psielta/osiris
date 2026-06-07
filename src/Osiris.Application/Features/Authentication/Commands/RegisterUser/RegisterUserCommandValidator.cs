using FluentValidation;

namespace Osiris.Application.Features.Authentication.Commands.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.TenantName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(100);

        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.Password)
            .WithMessage("Password confirmation must match the password.");
    }
}
