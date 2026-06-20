using Osiris.Application.Common.Csv;
using Osiris.Application.Features.FinancialAccountMovements.Commands.AnalyzeCsvImport;
using Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewCsvImport;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class CsvImportValidatorTests
{
    private readonly AnalyzeCsvImportCommandValidator _analyzeValidator = new();
    private readonly PreviewCsvImportCommandValidator _previewValidator = new();

    [Fact]
    public void Analyze_RejectsNonCsvExtension()
    {
        var result = _analyzeValidator.Validate(new AnalyzeCsvImportCommand(Guid.NewGuid(), [1], "extrato.pdf"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Analyze_AcceptsCsv()
    {
        var result = _analyzeValidator.Validate(new AnalyzeCsvImportCommand(Guid.NewGuid(), [1], "extrato.csv"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Preview_SignedMode_RequiresAmountColumn()
    {
        var mapping = new CsvImportMapping { AmountMode = CsvAmountMode.SignedAmount, AmountColumn = null };
        var result = _previewValidator.Validate(new PreviewCsvImportCommand(Guid.NewGuid(), [1], "extrato.csv", mapping));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Preview_DebitCreditMode_RequiresBothColumns()
    {
        var mapping = new CsvImportMapping { AmountMode = CsvAmountMode.DebitCredit, CreditColumn = 3, DebitColumn = null };
        var result = _previewValidator.Validate(new PreviewCsvImportCommand(Guid.NewGuid(), [1], "extrato.csv", mapping));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Preview_RejectsInvalidDecimalSeparator()
    {
        var mapping = new CsvImportMapping { AmountMode = CsvAmountMode.SignedAmount, AmountColumn = 2, DecimalSeparator = "x" };
        var result = _previewValidator.Validate(new PreviewCsvImportCommand(Guid.NewGuid(), [1], "extrato.csv", mapping));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Preview_AcceptsValidSignedMapping()
    {
        var mapping = new CsvImportMapping
        {
            AmountMode = CsvAmountMode.SignedAmount,
            DateColumn = 0,
            DescriptionColumn = 1,
            AmountColumn = 2,
        };
        var result = _previewValidator.Validate(new PreviewCsvImportCommand(Guid.NewGuid(), [1], "extrato.csv", mapping));
        Assert.True(result.IsValid);
    }
}
