using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Domain;

public sealed class FinancialCategoryTests
{
    [Fact]
    public void Archive_ShouldDeactivateAndStampUpdatedAt()
    {
        var category = new FinancialCategory(Guid.NewGuid(), "Food", CategoryType.Expense);
        var now = new DateTime(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc);

        category.Archive(now);

        Assert.False(category.IsActive);
        Assert.Equal(now, category.UpdatedAtUtc);
    }

    [Fact]
    public void Update_ShouldChangeFieldsAndResyncNormalizedName()
    {
        var category = new FinancialCategory(Guid.NewGuid(), "Food", CategoryType.Expense, "#111111");
        var now = new DateTime(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc);

        category.Update(" Salary ", CategoryType.Income, "#22AA33", now);

        Assert.Equal("Salary", category.Name);
        Assert.Equal("SALARY", category.NormalizedName);
        Assert.Equal(CategoryType.Income, category.Type);
        Assert.Equal("#22AA33", category.Color);
        Assert.Equal(now, category.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("Food", "FOOD")]
    [InlineData(" food ", "FOOD")]
    [InlineData("food", "FOOD")]
    public void NormalizeName_ShouldTrimAndUppercase(string input, string expected)
    {
        var normalized = FinancialCategory.NormalizeName(input);

        Assert.Equal(expected, normalized);
    }
}
