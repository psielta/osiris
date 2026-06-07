using FluentValidation;

namespace Osiris.Application.Features.Authentication.Commands.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.TenantName)
            .NotEmpty()
            .WithMessage("Informe o nome da área de trabalho.")
            .MaximumLength(100)
            .WithMessage("O nome da área de trabalho deve ter no máximo 100 caracteres.");

        RuleFor(command => command.FullName)
            .NotEmpty()
            .WithMessage("Informe seu nome completo.")
            .MaximumLength(100)
            .WithMessage("O nome completo deve ter no máximo 100 caracteres.");

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Informe o e-mail.")
            .EmailAddress()
            .WithMessage("Informe um e-mail válido.")
            .MaximumLength(256)
            .WithMessage("O e-mail deve ter no máximo 256 caracteres.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("Informe a senha.")
            .MinimumLength(6)
            .WithMessage("A senha deve ter pelo menos 6 caracteres.")
            .MaximumLength(100)
            .WithMessage("A senha deve ter no máximo 100 caracteres.");

        RuleFor(command => command.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Confirme a senha.")
            .Equal(command => command.Password)
            .WithMessage("A confirmação da senha deve ser igual à senha.");
    }
}
