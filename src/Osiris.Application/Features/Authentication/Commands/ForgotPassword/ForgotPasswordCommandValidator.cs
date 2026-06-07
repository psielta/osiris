using FluentValidation;

namespace Osiris.Application.Features.Authentication.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Informe o e-mail.")
            .EmailAddress()
            .WithMessage("Informe um e-mail válido.")
            .MaximumLength(256)
            .WithMessage("O e-mail deve ter no máximo 256 caracteres.");
    }
}
