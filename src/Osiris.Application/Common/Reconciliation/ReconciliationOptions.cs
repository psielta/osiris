namespace Osiris.Application.Common.Reconciliation;

/// <summary>
/// Tunables for matching imported statement lines against existing movements.
/// </summary>
/// <param name="DateToleranceDays">
/// Maximum absolute difference, in days, between an imported line and an existing movement for the pair
/// to be eligible. Covers a weekend plus 1-2 business days of bank posting delay.
/// </param>
/// <param name="ConfidentThreshold">
/// Minimum score for a near-date pair to be auto-suggested (pre-selected). Same-day exact matches are
/// always confident regardless of description.
/// </param>
public sealed record ReconciliationOptions(
    int DateToleranceDays = 3,
    double ConfidentThreshold = 0.72)
{
    public static readonly ReconciliationOptions Default = new();
}
