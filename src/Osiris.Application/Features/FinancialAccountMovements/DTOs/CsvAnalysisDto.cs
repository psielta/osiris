using Osiris.Application.Common.Csv;

namespace Osiris.Application.Features.FinancialAccountMovements.DTOs;

/// <summary>
/// Structure of an uploaded CSV plus any remembered mapping, used to drive the column-mapping UI.
/// Nothing is persisted at this stage.
/// </summary>
public sealed record CsvAnalysisDto(
    Guid AccountId,
    string AccountName,
    string Delimiter,
    string Encoding,
    int SuggestedHeaderLineIndex,
    IReadOnlyList<IReadOnlyList<string>> SampleRows,
    CsvImportMapping? SavedMapping);
