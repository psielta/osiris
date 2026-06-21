using Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewPdfImport;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class PdfImportValidatorTests
{
    private readonly PreviewPdfImportCommandValidator _validator = new();

    [Fact]
    public void RejectsNonPdfExtension()
    {
        var result = _validator.Validate(new PreviewPdfImportCommand(Guid.NewGuid(), [1], "extrato.csv"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RejectsEmptyContent()
    {
        var result = _validator.Validate(new PreviewPdfImportCommand(Guid.NewGuid(), [], "extrato.pdf"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AcceptsPdf()
    {
        var result = _validator.Validate(new PreviewPdfImportCommand(Guid.NewGuid(), [1], "extrato.pdf"));
        Assert.True(result.IsValid);
    }
}
