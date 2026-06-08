using Osiris.Application.Features.FinancialAccounts.Commands.CreateFinancialAccount;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccounts;

public sealed class CreateFinancialAccountCommandValidatorTests
{
    private readonly CreateFinancialAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var result = _validator.Validate(
            new CreateFinancialAccountCommand("Banco", FinancialAccountType.CheckingAccount, 0m));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenNameMissing_ShouldFail(string name)
    {
        var result = _validator.Validate(
            new CreateFinancialAccountCommand(name, FinancialAccountType.Cash, 0m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateFinancialAccountCommand.Name));
    }

    [Fact]
    public void Validate_WhenTypeMissing_ShouldFail()
    {
        var result = _validator.Validate(new CreateFinancialAccountCommand("Banco", null, 0m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateFinancialAccountCommand.Type));
    }

    [Fact]
    public void Validate_WhenTypeInvalid_ShouldFail()
    {
        var result = _validator.Validate(
            new CreateFinancialAccountCommand("Banco", (FinancialAccountType)99, 0m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateFinancialAccountCommand.Type));
    }

    [Fact]
    public void Validate_WhenInitialBalanceMissing_ShouldFail()
    {
        var result = _validator.Validate(new CreateFinancialAccountCommand("Banco", FinancialAccountType.Cash, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateFinancialAccountCommand.InitialBalance));
    }
}
