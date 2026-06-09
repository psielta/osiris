using FluentValidation;

namespace Osiris.Application.Features.Bills.Commands.UpdateBill;

public sealed class UpdateBillCommandValidator : AbstractValidator<UpdateBillCommand>
{
    public UpdateBillCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Conta inválida.");

        RuleFor(command => command.Description)
            .NotEmpty().WithMessage("Informe a descrição da conta.")
            .MaximumLength(200).WithMessage("A descrição deve ter no máximo 200 caracteres.");

        RuleFor(command => command.Amount)
            .NotNull().WithMessage("Informe o valor da conta.")
            .GreaterThan(0).WithMessage("O valor da conta deve ser maior que zero.")
            .Must(amount => amount is null || decimal.Round(amount.Value, 2) == amount.Value)
            .WithMessage("O valor da conta deve ter no máximo duas casas decimais.");

        RuleFor(command => command.DueDate)
            .NotNull().WithMessage("Informe a data de vencimento.");

        RuleFor(command => command.CategoryId)
            .NotNull().WithMessage("Selecione a categoria da conta.");

        RuleFor(command => command.Notes)
            .MaximumLength(500).WithMessage("As observações devem ter no máximo 500 caracteres.");
    }
}
