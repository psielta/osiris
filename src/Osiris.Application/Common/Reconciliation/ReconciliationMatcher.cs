using Osiris.Application.Common.Text;

namespace Osiris.Application.Common.Reconciliation;

/// <summary>
/// Matches imported statement lines against existing movements. Pure and deterministic.
///
/// Hard gates (a pair must pass all to be eligible): exact amount, same direction (inflow/outflow), and
/// date within <see cref="ReconciliationOptions.DateToleranceDays"/>. Amount is a hard gate on purpose so
/// reconciliation never hides a balance discrepancy. Eligible pairs are scored by date proximity and
/// description similarity for ranking; a same-day exact match (or a strong score) is auto-suggested via a
/// greedy one-to-one assignment so a single movement is never suggested to two lines.
/// </summary>
public static class ReconciliationMatcher
{
    public static IReadOnlyList<ReconciliationMatch> Match(
        IReadOnlyList<ReconciliationLine> lines,
        IReadOnlyList<ReconciliationCandidate> candidates,
        ReconciliationOptions options)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(options);

        if (lines.Count == 0 || candidates.Count == 0)
        {
            return [];
        }

        var pairs = new List<Pair>();
        foreach (var line in lines)
        {
            foreach (var candidate in candidates)
            {
                if (line.Amount != candidate.Amount || line.IsInflow != candidate.IsInflow)
                {
                    continue;
                }

                var dateDelta = Math.Abs(line.OccurredOn.DayNumber - candidate.OccurredOn.DayNumber);
                if (dateDelta > options.DateToleranceDays)
                {
                    continue;
                }

                var dateScore = 1.0 - ((double)dateDelta / (options.DateToleranceDays + 1));
                var descScore = TextSimilarity.Jaccard(line.Description, candidate.Description);
                var score = (0.5 * dateScore) + (0.5 * descScore);
                var isConfident = dateDelta == 0 || score >= options.ConfidentThreshold;

                pairs.Add(new Pair(line.RowKey, candidate.MovementId, score, dateDelta, descScore, candidate.OccurredOn, isConfident));
            }
        }

        if (pairs.Count == 0)
        {
            return [];
        }

        var suggested = AssignSuggestions(pairs);

        var results = new List<ReconciliationMatch>();
        foreach (var line in lines)
        {
            var lineCandidates = pairs
                .Where(pair => pair.RowKey == line.RowKey)
                .OrderByDescending(pair => pair.Score)
                .ThenBy(pair => pair.DateDelta)
                .ThenByDescending(pair => pair.DescScore)
                .ThenBy(pair => pair.CandidateDate)
                .ThenBy(pair => pair.MovementId)
                .Select(pair => new ReconciliationScoredCandidate(pair.MovementId, pair.Score, pair.IsConfident))
                .ToArray();

            if (lineCandidates.Length == 0)
            {
                continue;
            }

            var suggestedId = suggested.TryGetValue(line.RowKey, out var movementId) ? movementId : (Guid?)null;
            results.Add(new ReconciliationMatch(line.RowKey, suggestedId, lineCandidates));
        }

        return results;
    }

    // Greedy one-to-one over confident pairs: best score first, each line and movement used at most once.
    private static Dictionary<string, Guid> AssignSuggestions(List<Pair> pairs)
    {
        var ordered = pairs
            .Where(pair => pair.IsConfident)
            .OrderByDescending(pair => pair.Score)
            .ThenBy(pair => pair.DateDelta)
            .ThenByDescending(pair => pair.DescScore)
            .ThenBy(pair => pair.RowKey, StringComparer.Ordinal)
            .ThenBy(pair => pair.MovementId);

        var usedLines = new HashSet<string>(StringComparer.Ordinal);
        var usedMovements = new HashSet<Guid>();
        var suggested = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var pair in ordered)
        {
            if (usedLines.Contains(pair.RowKey) || usedMovements.Contains(pair.MovementId))
            {
                continue;
            }

            usedLines.Add(pair.RowKey);
            usedMovements.Add(pair.MovementId);
            suggested[pair.RowKey] = pair.MovementId;
        }

        return suggested;
    }

    private readonly record struct Pair(
        string RowKey,
        Guid MovementId,
        double Score,
        int DateDelta,
        double DescScore,
        DateOnly CandidateDate,
        bool IsConfident);
}
