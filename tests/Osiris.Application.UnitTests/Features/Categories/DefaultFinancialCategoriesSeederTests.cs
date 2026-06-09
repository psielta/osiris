using Osiris.Application.Features.Categories.Services;
using Osiris.Application.UnitTests.Features.Categories.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Categories;

public sealed class DefaultFinancialCategoriesSeederTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeCategoryRepository _categories = new();

    private DefaultFinancialCategoriesSeeder CreateSeeder() => new(_categories);

    private Task<IReadOnlyCollection<FinancialCategory>> ListAsync(Guid? tenantId = null)
    {
        return _categories.ListAsync(tenantId ?? _tenantId, includeArchived: true, CancellationToken.None);
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateDefaultIncomeAndExpenseCategories()
    {
        await CreateSeeder().SeedAsync(_tenantId, CancellationToken.None);

        var categories = await ListAsync();

        Assert.Equal(4, categories.Count(category => category.Type == CategoryType.Income));
        Assert.Equal(11, categories.Count(category => category.Type == CategoryType.Expense));
        Assert.Contains(categories, category =>
            category.Name == "Salário" && category.Type == CategoryType.Income);
        Assert.Contains(categories, category =>
            category.Name == "Moradia" && category.Type == CategoryType.Expense);
        Assert.Contains(categories, category =>
            category.Name == "Cartão - Encargos e Juros" && category.Type == CategoryType.Expense);
        Assert.All(categories, category => Assert.True(category.IsActive));
    }

    [Fact]
    public async Task SeedAsync_ShouldAssignCategoriesToTheGivenTenant()
    {
        await CreateSeeder().SeedAsync(_tenantId, CancellationToken.None);

        var categories = await ListAsync();

        Assert.All(categories, category => Assert.Equal(_tenantId, category.TenantId));
        Assert.Empty(await ListAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SeedAsync_WhenRunTwice_ShouldNotDuplicate()
    {
        var seeder = CreateSeeder();

        await seeder.SeedAsync(_tenantId, CancellationToken.None);
        await seeder.SeedAsync(_tenantId, CancellationToken.None);

        var categories = await ListAsync();
        Assert.Equal(15, categories.Count);
    }

    [Fact]
    public async Task SeedAsync_WhenCategoryAlreadyExists_ShouldKeepTheExistingOne()
    {
        var existing = new FinancialCategory(_tenantId, "moradia", CategoryType.Expense, "#000000");
        _categories.Add(existing);

        await CreateSeeder().SeedAsync(_tenantId, CancellationToken.None);

        var categories = await ListAsync();
        var housing = Assert.Single(categories, category =>
            category.NormalizedName == FinancialCategory.NormalizeName("Moradia"));
        Assert.Equal(existing.Id, housing.Id);
        Assert.Equal("#000000", housing.Color);
        Assert.Equal(15, categories.Count);
    }

    [Fact]
    public async Task SeedAsync_ShouldAllowSameNameAcrossDifferentTypes()
    {
        await CreateSeeder().SeedAsync(_tenantId, CancellationToken.None);

        var categories = await ListAsync();

        // "Outros" exists once as income and once as expense.
        Assert.Equal(2, categories.Count(category =>
            category.NormalizedName == FinancialCategory.NormalizeName("Outros")));
    }
}
