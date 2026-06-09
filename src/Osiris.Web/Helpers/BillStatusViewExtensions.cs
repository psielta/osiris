using Osiris.Domain.Enums;

namespace Osiris.Web.Helpers;

public static class BillStatusViewExtensions
{
    public static string ToDisplayName(this BillStatus status)
    {
        return status switch
        {
            BillStatus.Pending => "Pendente",
            BillStatus.Paid => "Paga",
            BillStatus.Overdue => "Vencida",
            _ => status.ToString()
        };
    }

    public static string ToBadgeClasses(this BillStatus status)
    {
        return status switch
        {
            BillStatus.Pending => "border-sky-200 bg-sky-50 text-sky-700",
            BillStatus.Paid => "border-emerald-300 bg-emerald-50 text-emerald-700",
            BillStatus.Overdue => "border-red-300 bg-red-50 text-red-700",
            _ => "border-slate-300 bg-slate-100 text-slate-700"
        };
    }
}
