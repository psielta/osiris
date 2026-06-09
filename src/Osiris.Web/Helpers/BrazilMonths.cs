using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Osiris.Web.Helpers;

public static class BrazilMonths
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string MonthName(int month)
    {
        var name = PtBr.DateTimeFormat.GetMonthName(month);
        return char.ToUpper(name[0], PtBr) + name[1..];
    }

    public static IReadOnlyCollection<SelectListItem> MonthSelectList(int selectedMonth)
    {
        return Enumerable.Range(1, 12)
            .Select(month => new SelectListItem
            {
                Text = MonthName(month),
                Value = month.ToString(),
                Selected = month == selectedMonth
            })
            .ToArray();
    }

    public static IReadOnlyCollection<SelectListItem> YearSelectList(int selectedYear)
    {
        var start = Math.Min(selectedYear, BrazilDates.Today().Year) - 3;
        var end = Math.Max(selectedYear, BrazilDates.Today().Year) + 3;

        return Enumerable.Range(start, end - start + 1)
            .Select(year => new SelectListItem
            {
                Text = year.ToString(),
                Value = year.ToString(),
                Selected = year == selectedYear
            })
            .ToArray();
    }
}
