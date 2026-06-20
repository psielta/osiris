using Osiris.Application.Features.FinancialAccountMovements.Commands.ImportOfxStatement;
using Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewOfxImport;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class OfxImportValidatorTests
{
    private readonly PreviewOfxImportCommandValidator _previewValidator = new();
    private readonly ImportOfxStatementCommandValidator _importValidator = new();

    [Fact]
    public void Preview_RejectsNonOfxExtension()
    {
        var result = _previewValidator.Validate(
            new PreviewOfxImportCommand(Guid.NewGuid(), [1], "extrato.pdf"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Preview_RejectsEmptyContent()
    {
        var result = _previewValidator.Validate(
            new PreviewOfxImportCommand(Guid.NewGuid(), [], "extrato.ofx"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Preview_AcceptsOfxFile()
    {
        var result = _previewValidator.Validate(
            new PreviewOfxImportCommand(Guid.NewGuid(), [1], "extrato.ofx"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Import_RejectsEmptyLines()
    {
        var result = _importValidator.Validate(
            new ImportOfxStatementCommand(Guid.NewGuid(), []));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Import_RejectsNonPositiveAmount()
    {
        var result = _importValidator.Validate(new ImportOfxStatementCommand(Guid.NewGuid(),
        [
            new ImportOfxLineInput("A1", new DateOnly(2026, 1, 1), 0m, FinancialAccountMovementType.Income, "x", null),
        ]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Import_AcceptsValidLine()
    {
        var result = _importValidator.Validate(new ImportOfxStatementCommand(Guid.NewGuid(),
        [
            new ImportOfxLineInput("A1", new DateOnly(2026, 1, 1), 10m, FinancialAccountMovementType.Income, "x", null),
        ]));

        Assert.True(result.IsValid);
    }
}
