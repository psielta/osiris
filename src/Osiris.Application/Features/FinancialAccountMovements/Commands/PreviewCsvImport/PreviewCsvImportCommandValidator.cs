using FluentValidation;
using Osiris.Application.Common.Csv;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewCsvImport;

public sealed class PreviewCsvImportCommandValidator : AbstractValidator<PreviewCsvImportCommand>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public PreviewCsvImportCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("Conta inválida.");

        RuleFor(command => command.Content)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Selecione um arquivo CSV para importar.")
            .Must(content => content.Length > 0).WithMessage("Selecione um arquivo CSV para importar.")
            .Must(content => content.LongLength <= MaxFileSizeBytes).WithMessage("O arquivo deve ter no máximo 5 MB.");

        RuleFor(command => command.Mapping)
            .NotNull().WithMessage("Mapeamento inválido.");

        When(command => command.Mapping is not null, () =>
        {
            RuleFor(command => command.Mapping.Delimiter)
                .NotEmpty().WithMessage("Selecione o separador de colunas.");

            RuleFor(command => command.Mapping.DateFormat)
                .NotEmpty().WithMessage("Informe o formato da data.");

            RuleFor(command => command.Mapping.DecimalSeparator)
                .Must(separator => separator is "," or ".").WithMessage("Separador decimal inválido.");

            RuleFor(command => command.Mapping.DateColumn)
                .GreaterThanOrEqualTo(0).WithMessage("Selecione a coluna da data.");

            RuleFor(command => command.Mapping.DescriptionColumn)
                .GreaterThanOrEqualTo(0).WithMessage("Selecione a coluna da descrição.");

            When(command => command.Mapping.AmountMode == CsvAmountMode.SignedAmount, () =>
            {
                RuleFor(command => command.Mapping.AmountColumn)
                    .NotNull().WithMessage("Selecione a coluna do valor.");
            });

            When(command => command.Mapping.AmountMode == CsvAmountMode.DebitCredit, () =>
            {
                RuleFor(command => command.Mapping.CreditColumn)
                    .NotNull().WithMessage("Selecione a coluna de crédito.");

                RuleFor(command => command.Mapping.DebitColumn)
                    .NotNull().WithMessage("Selecione a coluna de débito.");
            });

            When(command => command.Mapping.AmountMode == CsvAmountMode.TypeColumn, () =>
            {
                RuleFor(command => command.Mapping.AmountColumn)
                    .NotNull().WithMessage("Selecione a coluna do valor.");

                RuleFor(command => command.Mapping.TypeColumn)
                    .NotNull().WithMessage("Selecione a coluna do tipo.");
            });
        });
    }
}
