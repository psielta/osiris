using Osiris.Application.Features.Categories.Commands.CreateCategory;
using Osiris.Application.UnitTests.Features.Categories.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Categories;

public sealed class CreateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_ShouldCreateCategoryForCurrentTenant()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeCategoryRepository();
        var handler = new CreateCategoryCommandHandler(repository, new FakeCurrentUser(tenantId));

        var result = await handler.Handle(
            new CreateCategoryCommand("Food", CategoryType.Expense, "#A1B2C3"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var category = Assert.Single(repository.Categories);
        Assert.Equal(result.Value, category.Id);
        Assert.Equal(tenantId, category.TenantId);
        Assert.Equal("Food", category.Name);
        Assert.Equal("FOOD", category.NormalizedName);
        Assert.Equal(CategoryType.Expense, category.Type);
    }

    [Fact]
    public async Task Handle_WhenDuplicateExistsInSameTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeCategoryRepository();
        repository.Add(new FinancialCategory(tenantId, "Food", CategoryType.Expense));
        var handler = new CreateCategoryCommandHandler(repository, new FakeCurrentUser(tenantId));

        var result = await handler.Handle(
            new CreateCategoryCommand("Food", CategoryType.Expense, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(repository.Categories);
    }

    [Fact]
    public async Task Handle_WhenCaseVariantExistsInSameTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeCategoryRepository();
        repository.Add(new FinancialCategory(tenantId, "Food", CategoryType.Expense));
        var handler = new CreateCategoryCommandHandler(repository, new FakeCurrentUser(tenantId));

        var result = await handler.Handle(
            new CreateCategoryCommand("food", CategoryType.Expense, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(repository.Categories);
    }

    [Fact]
    public async Task Handle_WhenSameNameExistsInDifferentTenant_ShouldCreate()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeCategoryRepository();
        repository.Add(new FinancialCategory(Guid.NewGuid(), "Food", CategoryType.Expense));
        var handler = new CreateCategoryCommandHandler(repository, new FakeCurrentUser(tenantId));

        var result = await handler.Handle(
            new CreateCategoryCommand("Food", CategoryType.Expense, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, repository.Categories.Count);
    }
}
