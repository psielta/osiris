using FluentValidation;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;

public sealed class CreateManualMovementCommandValidator : AbstractValidator<CreateManualMovementCommand>
{
    public CreateManualMovementCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("Conta inválida.");

        RuleFor(command => command.Type)
            .NotNull().WithMessage("Selecione o tipo do lançamento.")
            .Must(type => type is FinancialAccountMovementType.Income or FinancialAccountMovementType.Expense)
            .WithMessage("Selecione um tipo de lançamento válido.");

        RuleFor(command => command.Amount)
            .NotNull().WithMessage("Informe o valor do lançamento.")
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(command => command.OccurredOn)
            .NotNull().WithMessage("Informe a data do lançamento.");

        RuleFor(command => command.Description)
            .NotEmpty().WithMessage("Informe a descrição do lançamento.")
            .MaximumLength(200).WithMessage("A descrição deve ter no máximo 200 caracteres.");

        RuleFor(command => command.Notes)
            .MaximumLength(500).WithMessage("As observações devem ter no máximo 500 caracteres.");
    }
}
