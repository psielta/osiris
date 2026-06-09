using FluentValidation;

namespace Osiris.Application.Features.Bills.Commands.MarkBillAsPaid;

public sealed class MarkBillAsPaidCommandValidator : AbstractValidator<MarkBillAsPaidCommand>
{
    public MarkBillAsPaidCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Conta inválida.");

        RuleFor(command => command.PaidAt)
            .NotNull().WithMessage("Informe a data do pagamento.");
    }
}
