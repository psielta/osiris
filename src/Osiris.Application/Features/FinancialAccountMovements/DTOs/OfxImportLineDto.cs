using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.DTOs;

/// <summary>
/// One transaction read from a statement (OFX/CSV/PDF), shown in the import preview. <see cref="Amount"/>
/// is always positive; <see cref="Type"/>/<see cref="IsInflow"/> carry the direction for display.
/// </summary>
/// <param name="SuggestedMovementId">
/// Existing movement auto-suggested for reconciliation (pre-selected), or null when there is no confident match.
/// </param>
/// <param name="Candidates">
/// Existing movements the line can be reconciled with, best-first; empty when there is no candidate.
/// </param>
public sealed record OfxImportLineDto(
    string RowKey,
    string ExternalId,
    DateOnly OccurredOn,
    decimal Amount,
    FinancialAccountMovementType Type,
    bool IsInflow,
    string Description,
    bool IsDuplicate,
    Guid? SuggestedMovementId,
    IReadOnlyList<ReconciliationCandidateDto> Candidates);
