namespace Osiris.Application.Common.Reconciliation;

/// <summary>
/// An existing account movement that an imported line could be reconciled with. Projected from a
/// <c>FinancialAccountMovement</c> so the matcher stays free of domain/EF coupling.
/// </summary>
public sealed record ReconciliationCandidate(
    Guid MovementId,
    DateOnly OccurredOn,
    decimal Amount,
    bool IsInflow,
    string Description);
