using Osiris.Application.Features.Categories.Commands.DeleteCategory;
using Osiris.Application.UnitTests.Features.Categories.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Categories;

public sealed class DeleteCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCategoryExists_ShouldDeleteIt()
    {
        var tenantId = Guid.NewGuid();
        var category = new FinancialCategory(tenantId, "Food", CategoryType.Expense);
        var repository = new FakeCategoryRepository();
        repository.Add(category);
        var handler = new DeleteCategoryCommandHandler(repository, new FakeCurrentUser(tenantId));

        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.Categories);
    }

    [Fact]
    public async Task Handle_WhenCategoryBelongsToOtherTenant_ShouldReturnNotFound()
    {
        var category = new FinancialCategory(Guid.NewGuid(), "Food", CategoryType.Expense);
        var repository = new FakeCategoryRepository();
        repository.Add(category);
        var handler = new DeleteCategoryCommandHandler(repository, new FakeCurrentUser(Guid.NewGuid()));

        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(repository.Categories);
    }
}
