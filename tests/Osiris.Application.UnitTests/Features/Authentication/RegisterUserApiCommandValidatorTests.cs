using Osiris.Application.Features.Authentication.Commands.RegisterUserApi;

namespace Osiris.Application.UnitTests.Features.Authentication;

public sealed class RegisterUserApiCommandValidatorTests
{
    private readonly RegisterUserApiCommandValidator _validator = new();

    private static RegisterUserApiCommand ValidCommand()
    {
        return new RegisterUserApiCommand("Acme Finance", "Jane Owner", "jane@osiris.test", "password1", "password1");
    }

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    public void Validate_WhenPasswordTooShortOrEmpty_ShouldFail(string password)
    {
        var result = _validator.Validate(ValidCommand() with { Password = password, ConfirmPassword = password });
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterUserApiCommand.Password));
    }

    [Fact]
    public void Validate_WhenConfirmPasswordMismatch_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { ConfirmPassword = "different1" });
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterUserApiCommand.ConfirmPassword));
    }

    [Fact]
    public void Validate_WhenTenantNameEmpty_ShouldFail()
    {
        var result = _validator.Validate(ValidCommand() with { TenantName = "" });
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterUserApiCommand.TenantName));
    }
}
