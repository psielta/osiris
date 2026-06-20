using FluentValidation;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.ImportOfxStatement;

public sealed class ImportOfxStatementCommandValidator : AbstractValidator<ImportOfxStatementCommand>
{
    public ImportOfxStatementCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("Conta inválida.");

        RuleFor(command => command.Lines)
            .NotEmpty().WithMessage("Selecione ao menos um lançamento para importar.");

        RuleForEach(command => command.Lines).SetValidator(new ImportOfxLineInputValidator());
    }
}

public sealed class ImportOfxLineInputValidator : AbstractValidator<ImportOfxLineInput>
{
    public ImportOfxLineInputValidator()
    {
        RuleFor(line => line.ExternalId)
            .NotEmpty().WithMessage("Lançamento sem identificador.");

        RuleFor(line => line.Amount)
            .GreaterThan(0).WithMessage("O valor do lançamento deve ser maior que zero.");

        RuleFor(line => line.Type)
            .Must(type => type is FinancialAccountMovementType.Income or FinancialAccountMovementType.Expense)
            .WithMessage("Tipo de lançamento inválido.");

        RuleFor(line => line.Description)
            .NotEmpty().WithMessage("Informe a descrição do lançamento.")
            .MaximumLength(200).WithMessage("A descrição deve ter no máximo 200 caracteres.");
    }
}
