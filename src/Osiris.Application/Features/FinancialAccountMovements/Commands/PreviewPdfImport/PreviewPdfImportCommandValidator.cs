using FluentValidation;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewPdfImport;

public sealed class PreviewPdfImportCommandValidator : AbstractValidator<PreviewPdfImportCommand>
{
    private const long MaxFileSizeBytes = 15 * 1024 * 1024;

    public PreviewPdfImportCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("Conta inválida.");

        RuleFor(command => command.Content)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Selecione um arquivo PDF para importar.")
            .Must(content => content.Length > 0).WithMessage("Selecione um arquivo PDF para importar.")
            .Must(content => content.LongLength <= MaxFileSizeBytes).WithMessage("O arquivo deve ter no máximo 15 MB.");

        RuleFor(command => command.FileName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Selecione um arquivo PDF para importar.")
            .Must(HasPdfExtension).WithMessage("Envie um arquivo no formato PDF (.pdf).");
    }

    private static bool HasPdfExtension(string fileName) =>
        fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
}
