using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Domain.Enums;

namespace Osiris.Web.Helpers;

public static class CategoryTypeViewExtensions
{
    public static IReadOnlyCollection<SelectListItem> SelectList()
    {
        return new[]
        {
            new SelectListItem { Text = "Receita", Value = ((int)CategoryType.Income).ToString() },
            new SelectListItem { Text = "Despesa", Value = ((int)CategoryType.Expense).ToString() }
        };
    }

    public static string ToDisplayName(this CategoryType type)
    {
        return type switch
        {
            CategoryType.Income => "Receita",
            CategoryType.Expense => "Despesa",
            _ => type.ToString()
        };
    }
}
