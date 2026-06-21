namespace Osiris.Web.Models;

/// <summary>
/// An existing movement offered as a reconciliation match for an imported line, shown in the preview's
/// candidate dropdown.
/// </summary>
public sealed record ReconciliationCandidateViewModel(
    Guid MovementId,
    DateOnly OccurredOn,
    decimal Amount,
    bool IsInflow,
    string Description);
