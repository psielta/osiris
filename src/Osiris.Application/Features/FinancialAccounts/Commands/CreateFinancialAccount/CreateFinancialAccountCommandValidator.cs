using FluentValidation;

namespace Osiris.Application.Features.FinancialAccounts.Commands.CreateFinancialAccount;

public sealed class CreateFinancialAccountCommandValidator : AbstractValidator<CreateFinancialAccountCommand>
{
    public CreateFinancialAccountCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Informe o nome da conta.")
            .MaximumLength(100).WithMessage("O nome da conta deve ter no máximo 100 caracteres.");

        RuleFor(command => command.Type)
            .NotNull().WithMessage("Selecione o tipo da conta.")
            .IsInEnum().WithMessage("Selecione um tipo de conta válido.");

        RuleFor(command => command.InitialBalance)
            .NotNull().WithMessage("Informe o saldo inicial.");
    }
}
