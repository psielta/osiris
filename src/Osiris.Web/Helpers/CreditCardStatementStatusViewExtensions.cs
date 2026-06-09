using Osiris.Domain.Enums;

namespace Osiris.Web.Helpers;

public static class CreditCardStatementStatusViewExtensions
{
    public static string ToDisplayName(this CreditCardStatementStatus status)
    {
        return status switch
        {
            CreditCardStatementStatus.Open => "Aberta",
            CreditCardStatementStatus.Closed => "Fechada",
            CreditCardStatementStatus.Paid => "Paga",
            CreditCardStatementStatus.PartiallyPaid => "Parcialmente paga",
            CreditCardStatementStatus.Overdue => "Vencida",
            _ => status.ToString()
        };
    }

    public static string ToBadgeClasses(this CreditCardStatementStatus status)
    {
        return status switch
        {
            CreditCardStatementStatus.Open => "border-sky-200 bg-sky-50 text-sky-700",
            CreditCardStatementStatus.Closed => "border-slate-300 bg-slate-100 text-slate-700",
            CreditCardStatementStatus.Paid => "border-emerald-300 bg-emerald-50 text-emerald-700",
            CreditCardStatementStatus.PartiallyPaid => "border-amber-300 bg-amber-50 text-amber-800",
            CreditCardStatementStatus.Overdue => "border-red-300 bg-red-50 text-red-700",
            _ => "border-slate-300 bg-slate-100 text-slate-700"
        };
    }

    public static string ToReferenceLabel(int referenceMonth, int referenceYear)
    {
        return $"{referenceMonth:00}/{referenceYear}";
    }
}
