namespace Osiris.Application.Common.Reconciliation;

/// <summary>
/// An imported statement line to be matched against existing movements. <see cref="RowKey"/> identifies
/// the line in the preview so the match can be mapped back to it.
/// </summary>
public sealed record ReconciliationLine(
    string RowKey,
    DateOnly OccurredOn,
    decimal Amount,
    bool IsInflow,
    string Description);
