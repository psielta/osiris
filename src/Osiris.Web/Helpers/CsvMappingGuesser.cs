using System.Globalization;

namespace Osiris.Web.Helpers;

/// <summary>
/// Best-effort guess of the date/description/value columns for the first import of a file (when there
/// is no remembered mapping). The user always reviews and can adjust before previewing.
/// </summary>
public static class CsvMappingGuesser
{
    private static readonly string[] DateFormats =
    {
        "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd/MM/yy", "MM/dd/yyyy"
    };

    public static (int DateColumn, int DescriptionColumn, int? AmountColumn) Guess(
        IReadOnlyList<IReadOnlyList<string>> sampleRows,
        int headerLineIndex,
        string decimalSeparator)
    {
        var width = sampleRows.Count > 0 ? sampleRows.Max(row => row.Count) : 0;
        var dataRows = sampleRows
            .Skip(headerLineIndex + 1)
            .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .Take(10)
            .ToList();

        if (width == 0 || dataRows.Count == 0)
        {
            return (0, width > 1 ? 1 : 0, width > 0 ? width - 1 : null);
        }

        var dateScores = new int[width];
        var amountScores = new int[width];
        var textScores = new int[width];

        foreach (var row in dataRows)
        {
            for (var column = 0; column < width; column++)
            {
                var cell = column < row.Count ? row[column]?.Trim() ?? string.Empty : string.Empty;
                if (cell.Length == 0)
                {
                    continue;
                }

                if (IsDate(cell))
                {
                    dateScores[column]++;
                }
                else if (IsAmount(cell, decimalSeparator))
                {
                    amountScores[column]++;
                }
                else
                {
                    textScores[column] += cell.Length;
                }
            }
        }

        var dateColumn = ArgMax(dateScores) ?? 0;
        var amountColumn = FirstPositive(amountScores, exclude: dateColumn);
        var descriptionColumn = ArgMax(textScores, exclude: new[] { dateColumn, amountColumn ?? -1 })
            ?? (width > 1 ? 1 : 0);

        return (dateColumn, descriptionColumn, amountColumn);
    }

    private static bool IsDate(string cell) =>
        DateOnly.TryParseExact(cell, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static bool IsAmount(string cell, string decimalSeparator) =>
        cell.Contains(decimalSeparator, StringComparison.Ordinal) && cell.Any(char.IsDigit);

    private static int? ArgMax(IReadOnlyList<int> scores, IReadOnlyCollection<int>? exclude = null)
    {
        int? best = null;
        var bestScore = 0;
        for (var index = 0; index < scores.Count; index++)
        {
            if (exclude is not null && exclude.Contains(index))
            {
                continue;
            }

            if (scores[index] > bestScore)
            {
                bestScore = scores[index];
                best = index;
            }
        }

        return best;
    }

    private static int? FirstPositive(IReadOnlyList<int> scores, int exclude)
    {
        for (var index = 0; index < scores.Count; index++)
        {
            if (index != exclude && scores[index] > 0)
            {
                return index;
            }
        }

        return null;
    }
}
