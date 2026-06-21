using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Reconciliation;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.Reconciliation;

/// <summary>
/// Enriches import-preview lines with reconciliation suggestions: looks up existing un-imported movements
/// in the statement's date window and, for each non-duplicate line, attaches the candidates and the
/// auto-suggested match. Shared by the OFX/CSV/PDF preview handlers so the logic lives in one place.
/// </summary>
public static class ImportReconciliationSuggester
{
    public static async Task<IReadOnlyList<OfxImportLineDto>> EnrichAsync(
        IFinancialAccountMovementRepository movements,
        Guid tenantId,
        Guid accountId,
        IReadOnlyList<OfxImportLineDto> lines,
        CancellationToken cancellationToken)
    {
        // Only lines that are not already imported (by external id) are worth reconciling.
        var reconcilable = lines.Where(line => !line.IsDuplicate).ToArray();
        if (reconcilable.Length == 0)
        {
            return lines;
        }

        var options = ReconciliationOptions.Default;
        var fromInclusive = reconcilable.Min(line => line.OccurredOn).AddDays(-options.DateToleranceDays);
        var toInclusive = reconcilable.Max(line => line.OccurredOn).AddDays(options.DateToleranceDays);

        var existing = await movements.ListReconciliationCandidatesAsync(
            tenantId, accountId, fromInclusive, toInclusive, cancellationToken);
        if (existing.Count == 0)
        {
            return lines;
        }

        var candidates = existing
            .Select(movement => new ReconciliationCandidate(
                movement.Id,
                movement.OccurredOn,
                movement.Amount,
                movement.Type.IsInflow(),
                movement.Description))
            .ToArray();
        var candidatesById = candidates.ToDictionary(candidate => candidate.MovementId);

        var matchLines = reconcilable
            .Select(line => new ReconciliationLine(
                line.RowKey,
                line.OccurredOn,
                line.Amount,
                line.IsInflow,
                line.Description))
            .ToArray();

        var matches = ReconciliationMatcher.Match(matchLines, candidates, options)
            .ToDictionary(match => match.RowKey, StringComparer.Ordinal);

        return lines
            .Select(line =>
            {
                if (!matches.TryGetValue(line.RowKey, out var match))
                {
                    return line;
                }

                var candidateDtos = match.Candidates
                    .Select(scored =>
                    {
                        var source = candidatesById[scored.MovementId];
                        return new ReconciliationCandidateDto(
                            source.MovementId,
                            source.OccurredOn,
                            source.Amount,
                            source.IsInflow,
                            source.Description,
                            scored.Score,
                            scored.IsConfident);
                    })
                    .ToArray();

                return line with
                {
                    SuggestedMovementId = match.SuggestedMovementId,
                    Candidates = candidateDtos,
                };
            })
            .ToArray();
    }
}
