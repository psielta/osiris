using Osiris.Application.Features.CreditCardPurchases.Services;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases;

public sealed class CreditCardInstallmentPlanBuilderTests
{
    [Fact]
    public void SplitAmount_WhenSingleInstallment_ShouldReturnTotal()
    {
        var amounts = CreditCardInstallmentPlanBuilder.SplitAmount(100m, 1);

        Assert.Equal(new[] { 100m }, amounts);
    }

    [Fact]
    public void SplitAmount_WhenDivisionIsExact_ShouldReturnEqualAmounts()
    {
        var amounts = CreditCardInstallmentPlanBuilder.SplitAmount(600m, 6);

        Assert.Equal(6, amounts.Count);
        Assert.All(amounts, amount => Assert.Equal(100m, amount));
    }

    [Theory]
    [InlineData(100, 3, "33.33", "33.34")]
    [InlineData(10, 3, "3.33", "3.34")]
    [InlineData(999.99, 2, "499.99", "500.00")]
    [InlineData(0.05, 4, "0.01", "0.02")]
    public void SplitAmount_WhenDivisionHasRemainder_ShouldPutDifferenceOnLastInstallment(
        decimal totalAmount,
        int installments,
        string expectedBase,
        string expectedLast)
    {
        var amounts = CreditCardInstallmentPlanBuilder.SplitAmount(totalAmount, installments);

        Assert.Equal(installments, amounts.Count);
        Assert.All(amounts.Take(installments - 1), amount => Assert.Equal(decimal.Parse(expectedBase), amount));
        Assert.Equal(decimal.Parse(expectedLast), amounts[^1]);
    }

    [Theory]
    [InlineData(100, 3)]
    [InlineData(0.05, 4)]
    [InlineData(123.45, 7)]
    [InlineData(1999.99, 12)]
    public void SplitAmount_ShouldAlwaysSumExactlyToTotal(decimal totalAmount, int installments)
    {
        var amounts = CreditCardInstallmentPlanBuilder.SplitAmount(totalAmount, installments);

        Assert.Equal(totalAmount, amounts.Sum());
    }

    [Fact]
    public void SplitAmount_WhenTotalTooSmall_ShouldProduceNonPositiveBaseInstallments()
    {
        var amounts = CreditCardInstallmentPlanBuilder.SplitAmount(0.01m, 2);

        Assert.Equal(new[] { 0m, 0.01m }, amounts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void SplitAmount_WhenTotalNotPositive_ShouldThrow(decimal totalAmount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreditCardInstallmentPlanBuilder.SplitAmount(totalAmount, 2));
    }

    [Fact]
    public void SplitAmount_WhenInstallmentsBelowOne_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreditCardInstallmentPlanBuilder.SplitAmount(100m, 0));
    }
}
