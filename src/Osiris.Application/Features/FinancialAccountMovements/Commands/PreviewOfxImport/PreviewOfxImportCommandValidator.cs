using FluentValidation;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewOfxImport;

public sealed class PreviewOfxImportCommandValidator : AbstractValidator<PreviewOfxImportCommand>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public PreviewOfxImportCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("Conta inválida.");

        RuleFor(command => command.Content)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Selecione um arquivo OFX para importar.")
            .Must(content => content.Length > 0).WithMessage("Selecione um arquivo OFX para importar.")
            .Must(content => content.LongLength <= MaxFileSizeBytes).WithMessage("O arquivo deve ter no máximo 5 MB.");

        RuleFor(command => command.FileName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Selecione um arquivo OFX para importar.")
            .Must(HasOfxExtension).WithMessage("Envie um arquivo no formato OFX (.ofx).");
    }

    private static bool HasOfxExtension(string fileName) =>
        fileName.EndsWith(".ofx", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".qfx", StringComparison.OrdinalIgnoreCase);
}
