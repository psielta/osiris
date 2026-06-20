namespace Osiris.Application.Common.Csv;

public interface ICsvStatementParser
{
    /// <summary>
    /// Inspects the file structure (delimiter, encoding, sample rows) to drive the mapping UI.
    /// Pass <paramref name="delimiterOverride"/>/<paramref name="encodingOverride"/> to re-read the
    /// sample with a user-chosen delimiter/encoding instead of the auto-detected ones.
    /// </summary>
    CsvAnalysisResult Analyze(byte[] content, string? delimiterOverride = null, string? encodingOverride = null);

    /// <summary>Applies a mapping and returns the normalized transactions, dropping rows that don't parse.</summary>
    IReadOnlyList<CsvStatementTransaction> Parse(byte[] content, CsvImportMapping mapping);
}
