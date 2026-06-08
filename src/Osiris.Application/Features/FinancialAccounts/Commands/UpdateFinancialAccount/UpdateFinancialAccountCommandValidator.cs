using FluentValidation;

namespace Osiris.Application.Features.FinancialAccounts.Commands.UpdateFinancialAccount;

public sealed class UpdateFinancialAccountCommandValidator : AbstractValidator<UpdateFinancialAccountCommand>
{
    public UpdateFinancialAccountCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Conta inválida.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Informe o nome da conta.")
            .MaximumLength(100).WithMessage("O nome da conta deve ter no máximo 100 caracteres.");

        RuleFor(command => command.Type)
            .NotNull().WithMessage("Selecione o tipo da conta.")
            .IsInEnum().WithMessage("Selecione um tipo de conta válido.");
    }
}
