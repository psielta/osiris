using Osiris.Application.Features.CreditCardPurchases.Commands.CreateCreditCardPurchase;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases;

public sealed class CreateCreditCardPurchaseCommandValidatorTests
{
    private readonly CreateCreditCardPurchaseCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenDescriptionEmpty_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Description = " " });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCreditCardPurchaseCommand.Description));
    }

    [Fact]
    public void Validate_WhenDescriptionTooLong_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Description = new string('a', 201) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCreditCardPurchaseCommand.Description));
    }

    [Fact]
    public void Validate_WhenCategoryMissing_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { CategoryId = null });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCreditCardPurchaseCommand.CategoryId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(-10.0)]
    public void Validate_WhenTotalAmountMissingOrNotPositive_ShouldFail(double? amount)
    {
        var result = _validator.Validate(ValidCommand() with { TotalAmount = (decimal?)amount });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCreditCardPurchaseCommand.TotalAmount));
    }

    [Fact]
    public void Validate_WhenTotalAmountHasMoreThanTwoDecimals_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { TotalAmount = 10.555m });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCreditCardPurchaseCommand.TotalAmount));
    }

    [Fact]
    public void Validate_WhenPurchaseDateMissing_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { PurchaseDate = null });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCreditCardPurchaseCommand.PurchaseDate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(121)]
    public void Validate_WhenInstallmentsMissingOrOutOfRange_ShouldFail(int? installments)
    {
        var result = _validator.Validate(ValidCommand() with { Installments = installments });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCreditCardPurchaseCommand.Installments));
    }

    [Fact]
    public void Validate_WhenNotesTooLong_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Notes = new string('a', 501) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCreditCardPurchaseCommand.Notes));
    }

    private static CreateCreditCardPurchaseCommand ValidCommand()
    {
        return new CreateCreditCardPurchaseCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Compra de mercado",
            150.50m,
            new DateOnly(2026, 6, 20),
            3,
            Notes: null);
    }
}
