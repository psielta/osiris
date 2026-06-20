using FluentValidation;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.AnalyzeCsvImport;

public sealed class AnalyzeCsvImportCommandValidator : AbstractValidator<AnalyzeCsvImportCommand>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public AnalyzeCsvImportCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("Conta inválida.");

        RuleFor(command => command.Content)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Selecione um arquivo CSV para importar.")
            .Must(content => content.Length > 0).WithMessage("Selecione um arquivo CSV para importar.")
            .Must(content => content.LongLength <= MaxFileSizeBytes).WithMessage("O arquivo deve ter no máximo 5 MB.");

        RuleFor(command => command.FileName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Selecione um arquivo CSV para importar.")
            .Must(HasCsvExtension).WithMessage("Envie um arquivo no formato CSV (.csv).");
    }

    private static bool HasCsvExtension(string fileName) =>
        fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
}
