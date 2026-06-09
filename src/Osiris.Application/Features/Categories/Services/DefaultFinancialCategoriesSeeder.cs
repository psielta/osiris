using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Categories.Services;

/// <summary>
/// Seeds the starter category set for a new tenant. Idempotent: a category whose normalized name
/// and type already exist for the tenant is skipped, so re-running never duplicates and never
/// overwrites user-edited categories.
/// </summary>
public sealed class DefaultFinancialCategoriesSeeder
{
    // "Cartão - Encargos e Juros" is for manually entered card charges (interest, fees) only —
    // regular statement payments must never be categorized as an expense.
    private static readonly (string Name, CategoryType Type, string Color)[] Defaults =
    {
        ("Salário", CategoryType.Income, "#10B981"),
        ("Freelance", CategoryType.Income, "#14B8A6"),
        ("Rendimentos", CategoryType.Income, "#0EA5E9"),
        ("Outros", CategoryType.Income, "#64748B"),
        ("Moradia", CategoryType.Expense, "#F59E0B"),
        ("Alimentação", CategoryType.Expense, "#EF4444"),
        ("Transporte", CategoryType.Expense, "#6366F1"),
        ("Saúde", CategoryType.Expense, "#EC4899"),
        ("Educação", CategoryType.Expense, "#8B5CF6"),
        ("Lazer", CategoryType.Expense, "#22C55E"),
        ("Assinaturas", CategoryType.Expense, "#06B6D4"),
        ("Compras", CategoryType.Expense, "#F97316"),
        ("Mercado", CategoryType.Expense, "#84CC16"),
        ("Cartão - Encargos e Juros", CategoryType.Expense, "#DC2626"),
        ("Outros", CategoryType.Expense, "#64748B")
    };

    private readonly ICategoryRepository _categories;

    public DefaultFinancialCategoriesSeeder(ICategoryRepository categories)
    {
        _categories = categories;
    }

    public async Task SeedAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var existing = await _categories.ListAsync(tenantId, includeArchived: true, cancellationToken);
        var existingKeys = existing
            .Select(category => (category.NormalizedName, category.Type))
            .ToHashSet();

        foreach (var (name, type, color) in Defaults)
        {
            if (existingKeys.Contains((FinancialCategory.NormalizeName(name), type)))
            {
                continue;
            }

            await _categories.AddAsync(new FinancialCategory(tenantId, name, type, color), cancellationToken);
        }
    }
}
