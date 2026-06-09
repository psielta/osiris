using Osiris.Application.Features.CreditCardStatementPayments.Commands.RegisterCreditCardStatementPayment;

namespace Osiris.Application.UnitTests.Features.CreditCardStatementPayments;

public sealed class RegisterCreditCardStatementPaymentCommandValidatorTests
{
    private readonly RegisterCreditCardStatementPaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(-50.0)]
    public void Validate_WhenAmountMissingOrNotPositive_ShouldFail(double? amount)
    {
        var result = _validator.Validate(ValidCommand() with { Amount = (decimal?)amount });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterCreditCardStatementPaymentCommand.Amount));
    }

    [Fact]
    public void Validate_WhenAmountHasMoreThanTwoDecimals_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Amount = 10.555m });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterCreditCardStatementPaymentCommand.Amount));
    }

    [Fact]
    public void Validate_WhenPaidAtMissing_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { PaidAt = null });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterCreditCardStatementPaymentCommand.PaidAt));
    }

    [Fact]
    public void Validate_WhenNotesTooLong_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { Notes = new string('a', 501) });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterCreditCardStatementPaymentCommand.Notes));
    }

    private static RegisterCreditCardStatementPaymentCommand ValidCommand()
    {
        return new RegisterCreditCardStatementPaymentCommand(
            Guid.NewGuid(),
            150.00m,
            new DateOnly(2026, 7, 1),
            FinancialAccountId: null,
            Notes: null);
    }
}
