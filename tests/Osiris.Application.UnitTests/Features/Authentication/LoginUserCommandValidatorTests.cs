using Osiris.Application.Features.Authentication.Commands.LoginUser;

namespace Osiris.Application.UnitTests.Features.Authentication;

public sealed class LoginUserCommandValidatorTests
{
    private readonly LoginUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPass()
    {
        var command = new LoginUserCommand("owner@osiris.test", "password1", RememberMe: true);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEmailAndPasswordAreEmpty_ShouldReturnExpectedErrors()
    {
        var command = new LoginUserCommand(string.Empty, string.Empty, RememberMe: false);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginUserCommand.Email));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginUserCommand.Password));
    }
}
