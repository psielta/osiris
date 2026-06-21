namespace Osiris.Application.Features.FinancialAccountMovements.DTOs;

/// <summary>
/// Outcome of a confirmed statement import.
/// </summary>
public sealed record OfxImportResultDto(
    int Imported,
    int Reconciled,
    int SkippedDuplicates,
    int Total);
