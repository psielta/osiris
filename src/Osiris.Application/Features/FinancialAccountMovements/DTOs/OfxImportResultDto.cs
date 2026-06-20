namespace Osiris.Application.Features.FinancialAccountMovements.DTOs;

/// <summary>
/// Outcome of a confirmed OFX import.
/// </summary>
public sealed record OfxImportResultDto(
    int Imported,
    int SkippedDuplicates,
    int Total);
