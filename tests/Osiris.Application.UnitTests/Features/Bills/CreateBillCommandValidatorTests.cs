using Osiris.Application.Features.Bills.Commands.CreateBill;

namespace Osiris.Application.UnitTests.Features.Bills;

public sealed class CreateBillCommandValidatorTests
{
    private readonly CreateBillCommandValidator _validator = new();

    private static CreateBillCommand ValidCommand()
    {
        return new CreateBillCommand(
            "Aluguel",
            1200m,
            new DateOnly(2026, 6, 10),
            Guid.NewGuid(),
            PaymentAccountId: null,
            Notes: null);
    }

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenDescriptionBlank_ShouldFail(string description)
    {
        var result = _validator.Validate(ValidCommand() with { Description = description });

        Assert.Contains(result.Errors, error => error.PropertyName == "Description");
    }

    [Fact]
    public void Validate_WhenDescriptionTooLong_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Description = new string('a', 201) });

        Assert.Contains(result.Errors, error => error.PropertyName == "Description");
    }

    [Fact]
    public void Validate_WhenAmountNull_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Amount = null });

        Assert.Contains(result.Errors, error => error.PropertyName == "Amount");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Validate_WhenAmountNotPositive_ShouldFail(decimal amount)
    {
        var result = _validator.Validate(ValidCommand() with { Amount = amount });

        Assert.Contains(result.Errors, error => error.PropertyName == "Amount");
    }

    [Fact]
    public void Validate_WhenAmountHasMoreThanTwoDecimals_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Amount = 10.123m });

        Assert.Contains(result.Errors, error => error.PropertyName == "Amount");
    }

    [Fact]
    public void Validate_WhenDueDateNull_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { DueDate = null });

        Assert.Contains(result.Errors, error => error.PropertyName == "DueDate");
    }

    [Fact]
    public void Validate_WhenCategoryNull_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { CategoryId = null });

        Assert.Contains(result.Errors, error => error.PropertyName == "CategoryId");
    }

    [Fact]
    public void Validate_WhenNotesTooLong_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Notes = new string('a', 501) });

        Assert.Contains(result.Errors, error => error.PropertyName == "Notes");
    }
}
