using Osiris.Application.Common.Models;
using Osiris.Application.Features.Categories.Commands.UpdateCategory;
using Osiris.Application.UnitTests.Features.Categories.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Categories;

public sealed class UpdateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateCategory()
    {
        var tenantId = Guid.NewGuid();
        var now = new DateTime(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc);
        var category = new FinancialCategory(tenantId, "Food", CategoryType.Expense);
        var repository = new FakeCategoryRepository();
        repository.Add(category);
        var handler = new UpdateCategoryCommandHandler(
            repository,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider { UtcNow = now });

        var result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, "Salary", CategoryType.Income, "#00AA00"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Salary", category.Name);
        Assert.Equal("SALARY", category.NormalizedName);
        Assert.Equal(CategoryType.Income, category.Type);
        Assert.Equal(now, category.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenCategoryBelongsToOtherTenant_ShouldReturnNotFound()
    {
        var tenantId = Guid.NewGuid();
        var category = new FinancialCategory(Guid.NewGuid(), "Food", CategoryType.Expense);
        var repository = new FakeCategoryRepository();
        repository.Add(category);
        var handler = new UpdateCategoryCommandHandler(
            repository,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, "Salary", CategoryType.Income, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
        Assert.Equal("Food", category.Name);
    }

    [Fact]
    public async Task Handle_WhenDuplicateExistsExcludingSelf_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var category = new FinancialCategory(tenantId, "Food", CategoryType.Expense);
        var duplicate = new FinancialCategory(tenantId, "Groceries", CategoryType.Expense);
        var repository = new FakeCategoryRepository();
        repository.Add(category);
        repository.Add(duplicate);
        var handler = new UpdateCategoryCommandHandler(
            repository,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, "Groceries", CategoryType.Expense, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Food", category.Name);
    }

    [Fact]
    public async Task Handle_WhenNameUnchanged_ShouldSucceed()
    {
        var tenantId = Guid.NewGuid();
        var category = new FinancialCategory(tenantId, "Food", CategoryType.Expense);
        var repository = new FakeCategoryRepository();
        repository.Add(category);
        var handler = new UpdateCategoryCommandHandler(
            repository,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, "Food", CategoryType.Expense, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
