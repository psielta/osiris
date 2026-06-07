using Osiris.Application.Features.Categories.Commands.ArchiveCategory;
using Osiris.Application.UnitTests.Features.Categories.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Categories;

public sealed class ArchiveCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCategoryExists_ShouldArchiveIt()
    {
        var tenantId = Guid.NewGuid();
        var now = new DateTime(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc);
        var category = new FinancialCategory(tenantId, "Food", CategoryType.Expense);
        var repository = new FakeCategoryRepository();
        repository.Add(category);
        var handler = new ArchiveCategoryCommandHandler(
            repository,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider { UtcNow = now });

        var result = await handler.Handle(new ArchiveCategoryCommand(category.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(category.IsActive);
        Assert.Equal(now, category.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenCategoryBelongsToOtherTenant_ShouldReturnNotFound()
    {
        var category = new FinancialCategory(Guid.NewGuid(), "Food", CategoryType.Expense);
        var repository = new FakeCategoryRepository();
        repository.Add(category);
        var handler = new ArchiveCategoryCommandHandler(
            repository,
            new FakeCurrentUser(Guid.NewGuid()),
            new FakeDateTimeProvider());

        var result = await handler.Handle(new ArchiveCategoryCommand(category.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.True(category.IsActive);
    }
}
