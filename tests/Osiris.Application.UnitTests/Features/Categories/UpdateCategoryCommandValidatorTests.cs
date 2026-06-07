using Osiris.Application.Features.Categories.Commands.UpdateCategory;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Categories;

public sealed class UpdateCategoryCommandValidatorTests
{
    private readonly UpdateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var result = _validator.Validate(
            new UpdateCategoryCommand(Guid.NewGuid(), "Food", CategoryType.Expense, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdMissing_ShouldFail()
    {
        var result = _validator.Validate(new UpdateCategoryCommand(Guid.Empty, "Food", CategoryType.Expense, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCategoryCommand.Id));
    }

    [Fact]
    public void Validate_WhenTypeMissing_ShouldFail()
    {
        var result = _validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), "Food", null, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCategoryCommand.Type));
    }

    [Fact]
    public void Validate_WhenTypeInvalid_ShouldFail()
    {
        var result = _validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), "Food", (CategoryType)999, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCategoryCommand.Type));
    }
}
