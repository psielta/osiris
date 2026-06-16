using Osiris.Application.Features.Dashboard.DTOs;

namespace Osiris.Web.Helpers;

/// <summary>
/// One slice of the dashboard spending doughnut. Colors and grouping are a presentation concern, so
/// this lives in the Web layer rather than in the Application query.
/// </summary>
public sealed record SpendingChartSlice(string Label, decimal Value, string Color);

public static class SpendingChartViewExtensions
{
    // Beyond this many categories the remainder collapses into a single "Outros" slice so the
    // doughnut stays readable.
    private const int MaxSlices = 8;

    private const string OtherLabel = "Outros";
    private const string OtherColor = "#94a3b8"; // slate-400

    // Brand-aligned fallbacks for categories without a configured color, cycled by position.
    private static readonly string[] FallbackPalette =
    {
        "#f59e0b", // amber-500
        "#10b981", // emerald-500
        "#0ea5e9", // sky-500
        "#8b5cf6", // violet-500
        "#f43f5e", // rose-500
        "#14b8a6", // teal-500
        "#eab308", // yellow-500
        "#6366f1", // indigo-500
    };

    /// <summary>
    /// Builds the doughnut slices from the spending-by-category data. Entries arrive already sorted
    /// by total (descending); zero-value categories are dropped and everything past the top
    /// <see cref="MaxSlices"/> is folded into a single "Outros" slice.
    /// </summary>
    public static IReadOnlyList<SpendingChartSlice> BuildSpendingChartSlices(
        this IReadOnlyCollection<SpendingByCategoryDto> spending)
    {
        var positive = spending.Where(entry => entry.Total > 0m).ToList();

        var slices = new List<SpendingChartSlice>(Math.Min(positive.Count, MaxSlices) + 1);
        var fallbackIndex = 0;

        for (var i = 0; i < positive.Count && i < MaxSlices; i++)
        {
            var entry = positive[i];
            var color = string.IsNullOrWhiteSpace(entry.CategoryColor)
                ? FallbackPalette[fallbackIndex++ % FallbackPalette.Length]
                : entry.CategoryColor;

            slices.Add(new SpendingChartSlice(entry.CategoryName, entry.Total, color));
        }

        if (positive.Count > MaxSlices)
        {
            var othersTotal = positive.Skip(MaxSlices).Sum(entry => entry.Total);
            slices.Add(new SpendingChartSlice(OtherLabel, othersTotal, OtherColor));
        }

        return slices;
    }
}
