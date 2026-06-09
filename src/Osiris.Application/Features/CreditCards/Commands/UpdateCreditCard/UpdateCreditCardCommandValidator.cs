using FluentValidation;

namespace Osiris.Application.Features.CreditCards.Commands.UpdateCreditCard;

public sealed class UpdateCreditCardCommandValidator : AbstractValidator<UpdateCreditCardCommand>
{
    public UpdateCreditCardCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Cartão inválido.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Informe o nome do cartão.")
            .MaximumLength(100).WithMessage("O nome do cartão deve ter no máximo 100 caracteres.");

        RuleFor(command => command.Limit)
            .NotNull().WithMessage("Informe o limite do cartão.")
            .GreaterThanOrEqualTo(0).WithMessage("O limite deve ser maior ou igual a zero.");

        RuleFor(command => command.ClosingDay)
            .NotNull().WithMessage("Informe o dia de fechamento.")
            .InclusiveBetween(1, 31).WithMessage("O dia de fechamento deve estar entre 1 e 31.");

        RuleFor(command => command.DueDay)
            .NotNull().WithMessage("Informe o dia de vencimento.")
            .InclusiveBetween(1, 31).WithMessage("O dia de vencimento deve estar entre 1 e 31.");
    }
}
