using Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class CreateManualMovementCommandValidatorTests
{
    private readonly CreateManualMovementCommandValidator _validator = new();

    private static CreateManualMovementCommand Valid() => new(
        Guid.NewGuid(),
        FinancialAccountMovementType.Income,
        10m,
        new DateOnly(2026, 6, 8),
        "Salário",
        CategoryId: null,
        Notes: null);

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData(FinancialAccountMovementType.BillPayment)]
    [InlineData(FinancialAccountMovementType.CreditCardStatementPayment)]
    [InlineData(FinancialAccountMovementType.TransferIn)]
    [InlineData(FinancialAccountMovementType.TransferOut)]
    [InlineData(FinancialAccountMovementType.Adjustment)]
    public void Validate_WhenTypeIsNotIncomeOrExpense_ShouldFail(FinancialAccountMovementType type)
    {
        AssertInvalid(Valid() with { Type = type }, nameof(CreateManualMovementCommand.Type));
    }

    [Fact]
    public void Validate_WhenTypeMissing_ShouldFail()
    {
        AssertInvalid(Valid() with { Type = null }, nameof(CreateManualMovementCommand.Type));
    }

    [Fact]
    public void Validate_WhenAmountMissing_ShouldFail()
    {
        AssertInvalid(Valid() with { Amount = null }, nameof(CreateManualMovementCommand.Amount));
    }

    [Fact]
    public void Validate_WhenAmountZero_ShouldFail()
    {
        AssertInvalid(Valid() with { Amount = 0m }, nameof(CreateManualMovementCommand.Amount));
    }

    [Fact]
    public void Validate_WhenAmountNegative_ShouldFail()
    {
        AssertInvalid(Valid() with { Amount = -1m }, nameof(CreateManualMovementCommand.Amount));
    }

    [Fact]
    public void Validate_WhenOccurredOnMissing_ShouldFail()
    {
        AssertInvalid(Valid() with { OccurredOn = null }, nameof(CreateManualMovementCommand.OccurredOn));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenDescriptionMissing_ShouldFail(string description)
    {
        AssertInvalid(Valid() with { Description = description }, nameof(CreateManualMovementCommand.Description));
    }

    private void AssertInvalid(CreateManualMovementCommand command, string property)
    {
        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == property);
    }
}
