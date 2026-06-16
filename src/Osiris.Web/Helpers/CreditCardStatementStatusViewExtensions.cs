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
            CreditCardStatementStatus.Open => "border-sky-200 bg-sky-50 text-sky-700 dark:border-sky-500/30 dark:bg-sky-500/10 dark:text-sky-300",
            CreditCardStatementStatus.Closed => "border-slate-300 bg-slate-100 text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300",
            CreditCardStatementStatus.Paid => "border-emerald-300 bg-emerald-50 text-emerald-700 dark:border-emerald-500/40 dark:bg-emerald-500/10 dark:text-emerald-300",
            CreditCardStatementStatus.PartiallyPaid => "border-amber-300 bg-amber-50 text-amber-800 dark:border-amber-500/40 dark:bg-amber-500/10 dark:text-amber-300",
            CreditCardStatementStatus.Overdue => "border-red-300 bg-red-50 text-red-700 dark:border-red-500/40 dark:bg-red-500/10 dark:text-red-300",
            _ => "border-slate-300 bg-slate-100 text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300"
        };
    }

    public static string ToReferenceLabel(int referenceMonth, int referenceYear)
    {
        return $"{referenceMonth:00}/{referenceYear}";
    }
}
