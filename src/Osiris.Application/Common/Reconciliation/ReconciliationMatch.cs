namespace Osiris.Application.Common.Reconciliation;

/// <summary>
/// Match result for a single imported line: the auto-suggested movement (if any) plus every eligible
/// candidate, best-first, so the user can also reconcile manually.
/// </summary>
public sealed record ReconciliationMatch(
    string RowKey,
    Guid? SuggestedMovementId,
    IReadOnlyList<ReconciliationScoredCandidate> Candidates);

/// <summary>
/// An eligible candidate for a line, with its match score and whether it is confident enough to be
/// auto-suggested.
/// </summary>
public sealed record ReconciliationScoredCandidate(
    Guid MovementId,
    double Score,
    bool IsConfident);
