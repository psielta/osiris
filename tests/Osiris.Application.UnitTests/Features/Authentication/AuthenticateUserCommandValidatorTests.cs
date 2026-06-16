using Osiris.Application.Features.Authentication.Commands.AuthenticateUser;

namespace Osiris.Application.UnitTests.Features.Authentication;

public sealed class AuthenticateUserCommandValidatorTests
{
    private readonly AuthenticateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var result = _validator.Validate(new AuthenticateUserCommand("jane@osiris.test", "password1"));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WhenEmailInvalid_ShouldFail(string email)
    {
        var result = _validator.Validate(new AuthenticateUserCommand(email, "password1"));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AuthenticateUserCommand.Email));
    }

    [Fact]
    public void Validate_WhenPasswordEmpty_ShouldFail()
    {
        var result = _validator.Validate(new AuthenticateUserCommand("jane@osiris.test", ""));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AuthenticateUserCommand.Password));
    }
}
