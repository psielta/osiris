using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Domain.Enums;

namespace Osiris.Web.Helpers;

public static class FinancialAccountTypeViewExtensions
{
    public static IReadOnlyCollection<SelectListItem> SelectList()
    {
        return new[]
        {
            new SelectListItem { Text = "Conta corrente", Value = ((int)FinancialAccountType.CheckingAccount).ToString() },
            new SelectListItem { Text = "Poupança", Value = ((int)FinancialAccountType.SavingsAccount).ToString() },
            new SelectListItem { Text = "Dinheiro", Value = ((int)FinancialAccountType.Cash).ToString() },
            new SelectListItem { Text = "Outra", Value = ((int)FinancialAccountType.Other).ToString() }
        };
    }

    public static string ToDisplayName(this FinancialAccountType type)
    {
        return type switch
        {
            FinancialAccountType.CheckingAccount => "Conta corrente",
            FinancialAccountType.SavingsAccount => "Poupança",
            FinancialAccountType.Cash => "Dinheiro",
            FinancialAccountType.Other => "Outra",
            _ => type.ToString()
        };
    }
}
