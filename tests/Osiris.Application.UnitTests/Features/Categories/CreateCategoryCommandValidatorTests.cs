using Osiris.Application.Features.Categories.Commands.CreateCategory;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Categories;

public sealed class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var result = _validator.Validate(new CreateCategoryCommand("Food", CategoryType.Expense, "#A1B2C3"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenNameMissing_ShouldFail(string name)
    {
        var result = _validator.Validate(new CreateCategoryCommand(name, CategoryType.Expense, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCategoryCommand.Name));
    }

    [Fact]
    public void Validate_WhenTypeMissing_ShouldFail()
    {
        var result = _validator.Validate(new CreateCategoryCommand("Food", null, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCategoryCommand.Type));
    }

    [Fact]
    public void Validate_WhenTypeInvalid_ShouldFail()
    {
        var result = _validator.Validate(new CreateCategoryCommand("Food", (CategoryType)999, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCategoryCommand.Type));
    }

    [Fact]
    public void Validate_WhenColorInvalid_ShouldFail()
    {
        var result = _validator.Validate(new CreateCategoryCommand("Food", CategoryType.Expense, "red"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCategoryCommand.Color));
    }
}
