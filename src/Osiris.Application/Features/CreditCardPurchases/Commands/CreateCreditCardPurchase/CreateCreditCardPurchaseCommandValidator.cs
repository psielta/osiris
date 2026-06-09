using FluentValidation;

namespace Osiris.Application.Features.CreditCardPurchases.Commands.CreateCreditCardPurchase;

public sealed class CreateCreditCardPurchaseCommandValidator : AbstractValidator<CreateCreditCardPurchaseCommand>
{
    public CreateCreditCardPurchaseCommandValidator()
    {
        RuleFor(command => command.CreditCardId)
            .NotEmpty().WithMessage("Cartão inválido.");

        RuleFor(command => command.CategoryId)
            .NotNull().WithMessage("Selecione a categoria da compra.");

        RuleFor(command => command.Description)
            .NotEmpty().WithMessage("Informe a descrição da compra.")
            .MaximumLength(200).WithMessage("A descrição deve ter no máximo 200 caracteres.");

        RuleFor(command => command.TotalAmount)
            .NotNull().WithMessage("Informe o valor total da compra.")
            .GreaterThan(0).WithMessage("O valor total deve ser maior que zero.")
            .Must(amount => amount is null || decimal.Round(amount.Value, 2) == amount.Value)
            .WithMessage("O valor total deve ter no máximo duas casas decimais.");

        RuleFor(command => command.PurchaseDate)
            .NotNull().WithMessage("Informe a data da compra.");

        RuleFor(command => command.Installments)
            .NotNull().WithMessage("Informe o número de parcelas.")
            .InclusiveBetween(1, 120).WithMessage("O número de parcelas deve estar entre 1 e 120.");

        RuleFor(command => command.Notes)
            .MaximumLength(500).WithMessage("As observações devem ter no máximo 500 caracteres.");
    }
}
