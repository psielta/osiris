namespace Osiris.Application.Features.FinancialAccountMovements.DTOs;

/// <summary>
/// An existing movement offered as a reconciliation match for an imported line, shown in the preview so
/// the user can accept the suggestion or pick a different one. Ordered best-first by the matcher.
/// </summary>
public sealed record ReconciliationCandidateDto(
    Guid MovementId,
    DateOnly OccurredOn,
    decimal Amount,
    bool IsInflow,
    string Description,
    double Score,
    bool IsConfident);
