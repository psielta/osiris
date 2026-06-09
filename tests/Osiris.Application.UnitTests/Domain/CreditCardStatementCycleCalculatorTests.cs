using Osiris.Domain.Services;

namespace Osiris.Application.UnitTests.Domain;

public sealed class CreditCardStatementCycleCalculatorTests
{
    [Fact]
    public void CalculateForPurchase_WhenPurchaseBeforeClosing_ShouldEnterCurrentStatement()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 6, 20), closingDay: 25, dueDay: 5);

        Assert.Equal(6, cycle.ReferenceMonth);
        Assert.Equal(2026, cycle.ReferenceYear);
        Assert.Equal(new DateOnly(2026, 6, 25), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2026, 7, 5), cycle.DueDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenPurchaseOnClosingDay_ShouldEnterCurrentStatement()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 6, 25), closingDay: 25, dueDay: 5);

        Assert.Equal(6, cycle.ReferenceMonth);
        Assert.Equal(2026, cycle.ReferenceYear);
        Assert.Equal(new DateOnly(2026, 6, 25), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2026, 7, 5), cycle.DueDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenPurchaseAfterClosing_ShouldEnterNextStatement()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 6, 26), closingDay: 25, dueDay: 5);

        Assert.Equal(7, cycle.ReferenceMonth);
        Assert.Equal(2026, cycle.ReferenceYear);
        Assert.Equal(new DateOnly(2026, 7, 25), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2026, 8, 5), cycle.DueDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenDueDayLessThanClosingDay_ShouldBeDueInMonthAfterClosing()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 3, 10), closingDay: 25, dueDay: 5);

        Assert.Equal(new DateOnly(2026, 3, 25), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2026, 4, 5), cycle.DueDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenDueDayEqualsClosingDay_ShouldBeDueInMonthAfterClosing()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 3, 10), closingDay: 25, dueDay: 25);

        Assert.Equal(new DateOnly(2026, 3, 25), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2026, 4, 25), cycle.DueDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenDueDayGreaterThanClosingDay_ShouldBeDueInSameMonthAsClosing()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 6, 8), closingDay: 10, dueDay: 20);

        Assert.Equal(6, cycle.ReferenceMonth);
        Assert.Equal(2026, cycle.ReferenceYear);
        Assert.Equal(new DateOnly(2026, 6, 10), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2026, 6, 20), cycle.DueDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenClosingDayBeyondFebruaryLength_ShouldUseLastValidDayOfMonth()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 2, 10), closingDay: 31, dueDay: 10);

        Assert.Equal(2, cycle.ReferenceMonth);
        Assert.Equal(2026, cycle.ReferenceYear);
        Assert.Equal(new DateOnly(2026, 2, 28), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2026, 3, 10), cycle.DueDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenLeapYearFebruary_ShouldUseFebruary29()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2028, 2, 10), closingDay: 31, dueDay: 10);

        Assert.Equal(new DateOnly(2028, 2, 29), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2028, 3, 10), cycle.DueDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenPurchaseOnClampedClosingDate_ShouldEnterCurrentStatement()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 2, 28), closingDay: 31, dueDay: 10);

        Assert.Equal(2, cycle.ReferenceMonth);
        Assert.Equal(new DateOnly(2026, 2, 28), cycle.ClosingDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenDueDayBeyondMonthLength_ShouldUseLastValidDayOfMonth()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 2, 3), closingDay: 5, dueDay: 31);

        Assert.Equal(new DateOnly(2026, 2, 5), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2026, 2, 28), cycle.DueDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenPurchaseAfterDecemberClosing_ShouldRollToJanuaryStatement()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 12, 26), closingDay: 25, dueDay: 5);

        Assert.Equal(1, cycle.ReferenceMonth);
        Assert.Equal(2027, cycle.ReferenceYear);
        Assert.Equal(new DateOnly(2027, 1, 25), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2027, 2, 5), cycle.DueDate);
    }

    [Fact]
    public void CalculateForPurchase_WhenDecemberStatementDueNextMonth_ShouldBeDueInJanuaryOfNextYear()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            new DateOnly(2026, 12, 20), closingDay: 25, dueDay: 5);

        Assert.Equal(12, cycle.ReferenceMonth);
        Assert.Equal(2026, cycle.ReferenceYear);
        Assert.Equal(new DateOnly(2026, 12, 25), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2027, 1, 5), cycle.DueDate);
    }

    [Fact]
    public void CalculateForReference_ShouldResolveCycleForGivenReference()
    {
        var cycle = CreditCardStatementCycleCalculator.CalculateForReference(
            referenceYear: 2026, referenceMonth: 12, closingDay: 25, dueDay: 5);

        Assert.Equal(12, cycle.ReferenceMonth);
        Assert.Equal(2026, cycle.ReferenceYear);
        Assert.Equal(new DateOnly(2026, 12, 25), cycle.ClosingDate);
        Assert.Equal(new DateOnly(2027, 1, 5), cycle.DueDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void CalculateForPurchase_WhenClosingDayInvalid_ShouldThrow(int closingDay)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreditCardStatementCycleCalculator.CalculateForPurchase(new DateOnly(2026, 6, 1), closingDay, dueDay: 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void CalculateForPurchase_WhenDueDayInvalid_ShouldThrow(int dueDay)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreditCardStatementCycleCalculator.CalculateForPurchase(new DateOnly(2026, 6, 1), closingDay: 25, dueDay));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void CalculateForReference_WhenMonthInvalid_ShouldThrow(int month)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreditCardStatementCycleCalculator.CalculateForReference(2026, month, closingDay: 25, dueDay: 5));
    }
}
