namespace Osiris.Application.Common.Csv;

/// <summary>
/// The result of inspecting a CSV file's structure so the user can build a column mapping: detected
/// delimiter/encoding, a suggested header row and a sample of the first parsed rows (as cells).
/// </summary>
public sealed record CsvAnalysisResult(
    string Delimiter,
    string Encoding,
    int SuggestedHeaderLineIndex,
    IReadOnlyList<IReadOnlyList<string>> SampleRows);
