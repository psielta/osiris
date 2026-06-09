using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Domain.Enums;

namespace Osiris.Web.Helpers;

public static class FinancialAccountMovementTypeViewExtensions
{
    public static IReadOnlyCollection<SelectListItem> ManualSelectList()
    {
        return new[]
        {
            new SelectListItem { Text = "Receita", Value = ((int)FinancialAccountMovementType.Income).ToString() },
            new SelectListItem { Text = "Despesa", Value = ((int)FinancialAccountMovementType.Expense).ToString() }
        };
    }

    public static string ToDisplayName(this FinancialAccountMovementType type)
    {
        return type switch
        {
            FinancialAccountMovementType.Income => "Receita",
            FinancialAccountMovementType.Expense => "Despesa",
            FinancialAccountMovementType.BillPayment => "Pagamento de conta",
            FinancialAccountMovementType.CreditCardStatementPayment => "Pagamento de fatura",
            FinancialAccountMovementType.TransferIn => "Transferência recebida",
            FinancialAccountMovementType.TransferOut => "Transferência enviada",
            FinancialAccountMovementType.Adjustment => "Ajuste",
            _ => type.ToString()
        };
    }
}
