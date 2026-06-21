using Osiris.Application.Common.Reconciliation;

namespace Osiris.Application.UnitTests.Common.Reconciliation;

public sealed class ReconciliationMatcherTests
{
    private static readonly DateOnly Day = new(2026, 6, 5);
    private static readonly ReconciliationOptions Options = ReconciliationOptions.Default;

    [Fact]
    public void Match_ExactSameDaySameDescription_IsConfidentWithTopScore()
    {
        var candidate = Cand(Day, 100m, inflow: true, "Salario");
        var matches = ReconciliationMatcher.Match(
            [Line("L1", Day, 100m, inflow: true, "Salario")],
            [candidate],
            Options);

        var match = Assert.Single(matches);
        Assert.Equal(candidate.MovementId, match.SuggestedMovementId);
        var scored = Assert.Single(match.Candidates);
        Assert.Equal(1.0, scored.Score);
        Assert.True(scored.IsConfident);
    }

    [Fact]
    public void Match_AmountDiffersByCents_NoMatch()
    {
        var matches = ReconciliationMatcher.Match(
            [Line("L1", Day, 100m, inflow: true, "Salario")],
            [Cand(Day, 100.01m, inflow: true, "Salario")],
            Options);

        Assert.Empty(matches);
    }

    [Fact]
    public void Match_DirectionMismatch_NoMatch()
    {
        var matches = ReconciliationMatcher.Match(
            [Line("L1", Day, 100m, inflow: true, "Salario")],
            [Cand(Day, 100m, inflow: false, "Salario")],
            Options);

        Assert.Empty(matches);
    }

    [Fact]
    public void Match_AtDateToleranceBoundary_IsCandidate_BeyondIsNot()
    {
        var withinCandidate = Cand(Day.AddDays(Options.DateToleranceDays), 100m, inflow: true, "Salario");

        var within = ReconciliationMatcher.Match(
            [Line("L1", Day, 100m, inflow: true, "Salario")],
            [withinCandidate],
            Options);
        Assert.Single(within);
        Assert.Equal(withinCandidate.MovementId, within[0].Candidates.Single().MovementId);

        var beyond = ReconciliationMatcher.Match(
            [Line("L1", Day, 100m, inflow: true, "Salario")],
            [Cand(Day.AddDays(Options.DateToleranceDays + 1), 100m, inflow: true, "Salario")],
            Options);
        Assert.Empty(beyond);
    }

    [Fact]
    public void Match_OneToOne_AssignsCandidateToBestLineOnly()
    {
        var candidate = Cand(Day, 100m, inflow: true, "Salario");
        var matches = ReconciliationMatcher.Match(
            [
                Line("L1", Day, 100m, inflow: true, "Salario"),  // identical description -> best
                Line("L2", Day, 100m, inflow: true, "Mercado"),  // weaker description
            ],
            [candidate],
            Options);

        var first = matches.Single(match => match.RowKey == "L1");
        var second = matches.Single(match => match.RowKey == "L2");

        Assert.Equal(candidate.MovementId, first.SuggestedMovementId);
        Assert.Null(second.SuggestedMovementId); // candidate already taken by L1
        Assert.Contains(second.Candidates, scored => scored.MovementId == candidate.MovementId); // still selectable manually
    }

    [Fact]
    public void Match_DescriptionTieBreak_RanksBetterDescriptionFirst()
    {
        var better = Cand(Day, 100m, inflow: true, "Mercado Livre");
        var worse = Cand(Day, 100m, inflow: true, "Mercado");

        var matches = ReconciliationMatcher.Match(
            [Line("L1", Day, 100m, inflow: true, "Mercado Livre")],
            [worse, better],
            Options);

        var match = Assert.Single(matches);
        Assert.Equal(better.MovementId, match.Candidates[0].MovementId);
        Assert.Equal(better.MovementId, match.SuggestedMovementId);
    }

    [Fact]
    public void Match_WeakNearDateScore_IsCandidateButNotAutoSuggested()
    {
        // Same amount/direction, 2 days apart, unrelated description -> eligible but below the confident bar.
        var candidate = Cand(Day.AddDays(2), 100m, inflow: true, "Pagamento aleatorio");
        var matches = ReconciliationMatcher.Match(
            [Line("L1", Day, 100m, inflow: true, "Salario")],
            [candidate],
            Options);

        var match = Assert.Single(matches);
        Assert.Null(match.SuggestedMovementId);
        var scored = Assert.Single(match.Candidates);
        Assert.False(scored.IsConfident);
    }

    [Fact]
    public void Match_EmptyDescriptions_DoesNotThrowAndMatchesOnDateAndAmount()
    {
        var candidate = Cand(Day, 100m, inflow: true, "   ");
        var matches = ReconciliationMatcher.Match(
            [Line("L1", Day, 100m, inflow: true, "")],
            [candidate],
            Options);

        var match = Assert.Single(matches);
        Assert.Equal(candidate.MovementId, match.SuggestedMovementId); // same-day exact amount is confident
    }

    private static ReconciliationLine Line(string rowKey, DateOnly date, decimal amount, bool inflow, string description) =>
        new(rowKey, date, amount, inflow, description);

    private static ReconciliationCandidate Cand(DateOnly date, decimal amount, bool inflow, string description) =>
        new(Guid.NewGuid(), date, amount, inflow, description);
}
