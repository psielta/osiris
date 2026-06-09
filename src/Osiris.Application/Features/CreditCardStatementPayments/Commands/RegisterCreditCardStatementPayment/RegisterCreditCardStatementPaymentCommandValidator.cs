using FluentValidation;

namespace Osiris.Application.Features.CreditCardStatementPayments.Commands.RegisterCreditCardStatementPayment;

public sealed class RegisterCreditCardStatementPaymentCommandValidator
    : AbstractValidator<RegisterCreditCardStatementPaymentCommand>
{
    public RegisterCreditCardStatementPaymentCommandValidator()
    {
        RuleFor(command => command.StatementId)
            .NotEmpty().WithMessage("Fatura inválida.");

        RuleFor(command => command.Amount)
            .NotNull().WithMessage("Informe o valor do pagamento.")
            .GreaterThan(0).WithMessage("O valor do pagamento deve ser maior que zero.")
            .Must(amount => amount is null || decimal.Round(amount.Value, 2) == amount.Value)
            .WithMessage("O valor do pagamento deve ter no máximo duas casas decimais.");

        RuleFor(command => command.PaidAt)
            .NotNull().WithMessage("Informe a data do pagamento.");

        RuleFor(command => command.Notes)
            .MaximumLength(500).WithMessage("As observações devem ter no máximo 500 caracteres.");
    }
}
