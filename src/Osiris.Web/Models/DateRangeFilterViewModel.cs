namespace Osiris.Web.Models;

public sealed class DateRangeFilterViewModel
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public DateOnly CurrentMonthFrom { get; init; }

    public DateOnly CurrentMonthTo { get; init; }

    public DateOnly NextMonthFrom { get; init; }

    public DateOnly NextMonthTo { get; init; }

    public string FromIso => From.ToString("yyyy-MM-dd");

    public string ToIso => To.ToString("yyyy-MM-dd");

    public string CurrentMonthFromIso => CurrentMonthFrom.ToString("yyyy-MM-dd");

    public string CurrentMonthToIso => CurrentMonthTo.ToString("yyyy-MM-dd");

    public string NextMonthFromIso => NextMonthFrom.ToString("yyyy-MM-dd");

    public string NextMonthToIso => NextMonthTo.ToString("yyyy-MM-dd");

    public string Label => From == To
        ? From.ToString("dd/MM/yyyy")
        : $"{From:dd/MM/yyyy} a {To:dd/MM/yyyy}";

    public bool IsCurrentMonth => From == CurrentMonthFrom && To == CurrentMonthTo;

    public bool IsNextMonth => From == NextMonthFrom && To == NextMonthTo;

    public static DateRangeFilterViewModel FromQuery(DateOnly today, DateOnly? from, DateOnly? to)
    {
        var currentMonthFrom = new DateOnly(today.Year, today.Month, 1);
        var currentMonthTo = currentMonthFrom.AddMonths(1).AddDays(-1);
        var nextMonthFrom = currentMonthFrom.AddMonths(1);
        var nextMonthTo = nextMonthFrom.AddMonths(1).AddDays(-1);

        var selectedFrom = from.HasValue && to.HasValue && from.Value <= to.Value
            ? from.Value
            : currentMonthFrom;
        var selectedTo = from.HasValue && to.HasValue && from.Value <= to.Value
            ? to.Value
            : currentMonthTo;

        return new DateRangeFilterViewModel
        {
            From = selectedFrom,
            To = selectedTo,
            CurrentMonthFrom = currentMonthFrom,
            CurrentMonthTo = currentMonthTo,
            NextMonthFrom = nextMonthFrom,
            NextMonthTo = nextMonthTo
        };
    }
}
