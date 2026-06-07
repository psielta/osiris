using FluentValidation;

namespace Osiris.Application.Features.Authentication.Commands.LoginUser;

public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Informe o e-mail.")
            .EmailAddress()
            .WithMessage("Informe um e-mail válido.")
            .MaximumLength(256)
            .WithMessage("O e-mail deve ter no máximo 256 caracteres.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("Informe a senha.");
    }
}
