using Microsoft.AspNetCore.Mvc.Rendering;

namespace Osiris.Web.Helpers;

public static class DayOfMonthViewExtensions
{
    public static IReadOnlyCollection<SelectListItem> DaySelectList()
    {
        return Enumerable.Range(1, 31)
            .Select(day => new SelectListItem(day.ToString(), day.ToString()))
            .ToArray();
    }
}
