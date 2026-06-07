using Osiris.Application.Features.Authentication.Commands.RegisterUser;

namespace Osiris.Application.UnitTests.Features.Authentication;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPass()
    {
        var command = new RegisterUserCommand(
            "Acme Finance",
            "Jane Owner",
            "jane@osiris.test",
            "password1",
            "password1");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPasswordConfirmationDoesNotMatch_ShouldReturnExpectedError()
    {
        var command = new RegisterUserCommand(
            "Acme Finance",
            "Jane Owner",
            "jane@osiris.test",
            "password1",
            "different1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterUserCommand.ConfirmPassword) &&
            error.ErrorMessage == "A confirmação da senha deve ser igual à senha.");
    }

    [Fact]
    public void Validate_WhenRequiredFieldsAreMissing_ShouldReturnExpectedErrors()
    {
        var command = new RegisterUserCommand(string.Empty, string.Empty, "not-an-email", string.Empty, string.Empty);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterUserCommand.TenantName));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterUserCommand.FullName));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterUserCommand.Email));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterUserCommand.Password));
    }
}
