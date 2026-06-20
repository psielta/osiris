namespace Osiris.Application.Features.FinancialAccountMovements.DTOs;

/// <summary>
/// Preview of an OFX import: the parsed transactions (with duplicates flagged) plus header metadata
/// read from the file. Nothing is persisted until the user confirms.
/// </summary>
public sealed record OfxImportPreviewDto(
    Guid AccountId,
    string AccountName,
    string? BankId,
    string? AccountNumber,
    string? CurrencyCode,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    int TotalCount,
    int NewCount,
    int DuplicateCount,
    IReadOnlyList<OfxImportLineDto> Lines);
