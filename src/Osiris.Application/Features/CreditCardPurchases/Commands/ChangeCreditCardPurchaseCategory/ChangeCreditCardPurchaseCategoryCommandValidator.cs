using FluentValidation;

namespace Osiris.Application.Features.CreditCardPurchases.Commands.ChangeCreditCardPurchaseCategory;

public sealed class ChangeCreditCardPurchaseCategoryCommandValidator
    : AbstractValidator<ChangeCreditCardPurchaseCategoryCommand>
{
    public ChangeCreditCardPurchaseCategoryCommandValidator()
    {
        RuleFor(command => command.PurchaseId)
            .NotEmpty().WithMessage("Informe a compra.");

        RuleFor(command => command.CategoryId)
            .NotEmpty().WithMessage("Informe a categoria.");
    }
}
