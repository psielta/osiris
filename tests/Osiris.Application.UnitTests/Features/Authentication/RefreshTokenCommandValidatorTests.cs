using Osiris.Application.Features.Authentication.Commands.RefreshToken;

namespace Osiris.Application.UnitTests.Features.Authentication;

public sealed class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTokenPresent_ShouldPass()
    {
        var result = _validator.Validate(new RefreshTokenCommand("some-token"));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenTokenBlank_ShouldFail(string token)
    {
        var result = _validator.Validate(new RefreshTokenCommand(token));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RefreshTokenCommand.RefreshToken));
    }
}
