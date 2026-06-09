using Osiris.Application.Features.CreditCards.Commands.CreateCreditCard;

namespace Osiris.Application.UnitTests.Features.CreditCards;

public sealed class CreateCreditCardCommandValidatorTests
{
    private readonly CreateCreditCardCommandValidator _validator = new();

    private static CreateCreditCardCommand Valid() => new("Nubank", 1500m, 3, 10, null);

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenNameMissing_ShouldFail(string name)
    {
        AssertInvalid(Valid() with { Name = name }, nameof(CreateCreditCardCommand.Name));
    }

    [Fact]
    public void Validate_WhenLimitMissing_ShouldFail()
    {
        AssertInvalid(Valid() with { Limit = null }, nameof(CreateCreditCardCommand.Limit));
    }

    [Fact]
    public void Validate_WhenLimitNegative_ShouldFail()
    {
        AssertInvalid(Valid() with { Limit = -1m }, nameof(CreateCreditCardCommand.Limit));
    }

    [Fact]
    public void Validate_WhenClosingDayMissing_ShouldFail()
    {
        AssertInvalid(Valid() with { ClosingDay = null }, nameof(CreateCreditCardCommand.ClosingDay));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Validate_WhenClosingDayOutOfRange_ShouldFail(int day)
    {
        AssertInvalid(Valid() with { ClosingDay = day }, nameof(CreateCreditCardCommand.ClosingDay));
    }

    [Fact]
    public void Validate_WhenDueDayMissing_ShouldFail()
    {
        AssertInvalid(Valid() with { DueDay = null }, nameof(CreateCreditCardCommand.DueDay));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Validate_WhenDueDayOutOfRange_ShouldFail(int day)
    {
        AssertInvalid(Valid() with { DueDay = day }, nameof(CreateCreditCardCommand.DueDay));
    }

    private void AssertInvalid(CreateCreditCardCommand command, string property)
    {
        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == property);
    }
}
