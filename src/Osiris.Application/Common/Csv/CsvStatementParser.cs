using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Osiris.Domain.Enums;

namespace Osiris.Application.Common.Csv;

/// <summary>
/// Parses delimited bank statement exports (CSV) using a user-supplied column mapping. Like the OFX
/// parser it is tolerant: rows that don't yield a valid date and a parseable amount are simply
/// dropped, which naturally discards preamble, totals and running-balance lines. Amounts are
/// normalized to a positive value plus a direction (<see cref="FinancialAccountMovementType"/>).
/// </summary>
public sealed partial class CsvStatementParser : ICsvStatementParser
{
    private const int MaxDescriptionLength = 200;
    private const int MaxExternalIdLength = 255;
    private const int SampleRowCount = 30;
    private const int DetectionLineCount = 50;
    private const string FallbackDescription = "Lançamento importado";

    private static readonly string[] FallbackDateFormats =
    {
        "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd/MM/yy", "MM/dd/yyyy"
    };

    static CsvStatementParser()
    {
        // Windows-1252 is not available out of the box on every runtime (Brazilian exports are often cp1252).
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public CsvAnalysisResult Analyze(byte[] content, string? delimiterOverride = null, string? encodingOverride = null)
    {
        if (content is null || content.Length == 0)
        {
            return new CsvAnalysisResult(";", "utf-8", 0, []);
        }

        var encoding = string.IsNullOrWhiteSpace(encodingOverride)
            ? DetectEncoding(content)
            : ResolveEncoding(encodingOverride);

        var wireDelimiter = string.IsNullOrWhiteSpace(delimiterOverride)
            ? ToWireDelimiter(DetectDelimiter(SplitLines(encoding.GetString(StripBom(content))).Take(DetectionLineCount).ToList()))
            : delimiterOverride.Trim();

        var rows = ReadRows(content, encoding, NormalizeDelimiter(wireDelimiter), SampleRowCount);
        var headerIndex = SuggestHeaderLineIndex(rows);

        return new CsvAnalysisResult(wireDelimiter, encoding.WebName, headerIndex, rows);
    }

    public IReadOnlyList<CsvStatementTransaction> Parse(byte[] content, CsvImportMapping mapping)
    {
        if (content is null || content.Length == 0)
        {
            return [];
        }

        var encoding = ResolveEncoding(mapping.Encoding);
        var rows = ReadRows(content, encoding, NormalizeDelimiter(mapping.Delimiter), int.MaxValue);

        var dataStart = mapping.HasHeader ? mapping.HeaderLineIndex + 1 : mapping.HeaderLineIndex;
        if (dataStart < 0)
        {
            dataStart = 0;
        }

        var transactions = new List<CsvStatementTransaction>();
        for (var index = dataStart; index < rows.Count; index++)
        {
            var transaction = MapRow(rows[index], mapping);
            if (transaction is not null)
            {
                transactions.Add(transaction);
            }
        }

        return transactions;
    }

    private static CsvStatementTransaction? MapRow(IReadOnlyList<string> row, CsvImportMapping mapping)
    {
        var occurredOn = ParseDate(GetCell(row, mapping.DateColumn), mapping.DateFormat);
        if (occurredOn is null)
        {
            return null;
        }

        var resolved = ResolveAmount(row, mapping);
        if (resolved is null)
        {
            return null;
        }

        var (signedAmount, type) = resolved.Value;
        var description = BuildDescription(row, mapping);
        var externalId = ResolveExternalId(row, mapping, occurredOn.Value, signedAmount, description);

        return new CsvStatementTransaction(externalId, occurredOn.Value, Math.Abs(signedAmount), type, description);
    }

    private static (decimal SignedAmount, FinancialAccountMovementType Type)? ResolveAmount(
        IReadOnlyList<string> row,
        CsvImportMapping mapping)
    {
        switch (mapping.AmountMode)
        {
            case CsvAmountMode.DebitCredit:
            {
                var credit = ParseAmount(GetCell(row, mapping.CreditColumn), mapping.DecimalSeparator);
                if (credit is decimal creditValue && creditValue != 0m)
                {
                    return (Math.Abs(creditValue), FinancialAccountMovementType.Income);
                }

                var debit = ParseAmount(GetCell(row, mapping.DebitColumn), mapping.DecimalSeparator);
                if (debit is decimal debitValue && debitValue != 0m)
                {
                    return (-Math.Abs(debitValue), FinancialAccountMovementType.Expense);
                }

                return null;
            }

            case CsvAmountMode.TypeColumn:
            {
                var value = ParseAmount(GetCell(row, mapping.AmountColumn), mapping.DecimalSeparator);
                if (value is null || value.Value == 0m)
                {
                    return null;
                }

                var magnitude = Math.Abs(value.Value);
                var type = ClassifyType(GetCell(row, mapping.TypeColumn), mapping)
                    ?? (value.Value < 0m ? FinancialAccountMovementType.Expense : FinancialAccountMovementType.Income);
                var signed = type == FinancialAccountMovementType.Expense ? -magnitude : magnitude;
                return (signed, type);
            }

            default:
            {
                var value = ParseAmount(GetCell(row, mapping.AmountColumn), mapping.DecimalSeparator);
                if (value is null || value.Value == 0m)
                {
                    return null;
                }

                var type = value.Value < 0m
                    ? FinancialAccountMovementType.Expense
                    : FinancialAccountMovementType.Income;
                return (value.Value, type);
            }
        }
    }

    private static FinancialAccountMovementType? ClassifyType(string? text, CsvImportMapping mapping)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = StripDiacritics(text.Trim().ToUpperInvariant());
        if (MatchesToken(normalized, mapping.ExpenseTokens))
        {
            return FinancialAccountMovementType.Expense;
        }

        if (MatchesToken(normalized, mapping.IncomeTokens))
        {
            return FinancialAccountMovementType.Income;
        }

        return null;
    }

    private static bool MatchesToken(string normalized, IReadOnlyList<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            var normalizedToken = StripDiacritics(token.Trim().ToUpperInvariant());
            if (normalizedToken.Length > 0 && normalized.StartsWith(normalizedToken, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildDescription(IReadOnlyList<string> row, CsvImportMapping mapping)
    {
        var parts = new[] { GetCell(row, mapping.DescriptionColumn), GetCell(row, mapping.SecondaryDescriptionColumn) }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());
        var combined = WhitespaceRegex().Replace(string.Join(" - ", parts), " ").Trim();

        if (combined.Length == 0)
        {
            combined = FallbackDescription;
        }

        return combined.Length > MaxDescriptionLength ? combined[..MaxDescriptionLength] : combined;
    }

    private static string ResolveExternalId(
        IReadOnlyList<string> row,
        CsvImportMapping mapping,
        DateOnly occurredOn,
        decimal signedAmount,
        string description)
    {
        var external = GetCell(row, mapping.ExternalIdColumn)?.Trim();
        if (!string.IsNullOrWhiteSpace(external))
        {
            return external.Length > MaxExternalIdLength ? external[..MaxExternalIdLength] : external;
        }

        // No stable id from the bank: synthesize a key so re-importing the same statement still de-dupes.
        var seed = $"{occurredOn:yyyyMMdd}|{signedAmount.ToString(CultureInfo.InvariantCulture)}|{description}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed)));
        return $"csv-syn:{hash[..32]}";
    }

    private static decimal? ParseAmount(string? value, string decimalSeparator)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var raw = value.Trim();

        // Accounting negatives are sometimes written as (1.234,56).
        var negative = raw.StartsWith('(') && raw.EndsWith(')');
        if (negative)
        {
            raw = raw[1..^1].Trim();
        }

        raw = decimalSeparator == "."
            ? raw.Replace(",", string.Empty)
            : raw.Replace(".", string.Empty).Replace(',', '.');

        // Drop currency symbols, spaces and any other noise, keeping only the number.
        raw = AmountCleanupRegex().Replace(raw, string.Empty);
        if (raw.Length == 0 || raw is "-" or "+" or ".")
        {
            return null;
        }

        if (!decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
        {
            return null;
        }

        return negative ? -Math.Abs(amount) : amount;
    }

    private static DateOnly? ParseDate(string? value, string format)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var raw = value.Trim();
        if (!string.IsNullOrWhiteSpace(format)
            && DateOnly.TryParseExact(raw, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
        {
            return exact;
        }

        return DateOnly.TryParseExact(raw, FallbackDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static string? GetCell(IReadOnlyList<string> row, int? index)
    {
        if (index is not int position || position < 0 || position >= row.Count)
        {
            return null;
        }

        var value = row[position];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int SuggestHeaderLineIndex(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            if (!LooksLikeDataRow(rows[index]))
            {
                continue;
            }

            // The header is the closest non-empty row above the first data row.
            for (var above = index - 1; above >= 0; above--)
            {
                if (rows[above].Any(cell => !string.IsNullOrWhiteSpace(cell)))
                {
                    return above;
                }
            }

            return 0;
        }

        return 0;
    }

    private static bool LooksLikeDataRow(IReadOnlyList<string> row) =>
        row.Any(LooksLikeDate) && row.Any(LooksLikeAmount);

    private static bool LooksLikeDate(string? cell) =>
        !string.IsNullOrWhiteSpace(cell)
        && DateOnly.TryParseExact(cell.Trim(), FallbackDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static bool LooksLikeAmount(string? cell) =>
        !string.IsNullOrWhiteSpace(cell) && AmountProbeRegex().IsMatch(cell.Trim());

    private static List<IReadOnlyList<string>> ReadRows(byte[] content, Encoding encoding, string delimiter, int max)
    {
        var rows = new List<IReadOnlyList<string>>();
        using var stream = new MemoryStream(content);
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            HasHeaderRecord = false,
            DetectColumnCountChanges = false,
            IgnoreBlankLines = false,
            TrimOptions = TrimOptions.None,
            BadDataFound = null,
            MissingFieldFound = null,
        };

        using var parser = new CsvParser(reader, configuration);
        while (rows.Count < max && parser.Read())
        {
            rows.Add(parser.Record?.ToArray() ?? []);
        }

        return rows;
    }

    private static Encoding DetectEncoding(byte[] content)
    {
        if (HasUtf8Bom(content))
        {
            return Encoding.UTF8;
        }

        try
        {
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(content);
            return Encoding.UTF8;
        }
        catch (DecoderFallbackException)
        {
            return Windows1252();
        }
    }

    private static Encoding ResolveEncoding(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Encoding.UTF8;
        }

        var normalized = name.Trim();
        if (normalized.Contains("UTF", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.UTF8;
        }

        if (normalized.Contains("1252", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("WINDOWS", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("ANSI", StringComparison.OrdinalIgnoreCase))
        {
            return Windows1252();
        }

        if (normalized.Contains("8859", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("LATIN", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.Latin1;
        }

        try
        {
            return Encoding.GetEncoding(normalized);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    private static Encoding Windows1252()
    {
        try
        {
            return Encoding.GetEncoding(1252);
        }
        catch (ArgumentException)
        {
            return Encoding.Latin1;
        }
    }

    private static bool HasUtf8Bom(byte[] content) =>
        content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF;

    private static byte[] StripBom(byte[] content) => HasUtf8Bom(content) ? content[3..] : content;

    private static IReadOnlyList<string> SplitLines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    private static string NormalizeDelimiter(string? delimiter)
    {
        if (string.IsNullOrEmpty(delimiter))
        {
            return ";";
        }

        return delimiter is "\\t" or "tab" or "TAB" ? "\t" : delimiter;
    }

    // The wire/UI represents a tab delimiter as the literal two-character token "\t".
    private static string ToWireDelimiter(string actual) => actual == "\t" ? "\\t" : actual;

    private static string DetectDelimiter(IReadOnlyList<string> lines)
    {
        string[] candidates = { ";", ",", "\t" };
        var best = ";";
        var bestScore = -1;

        foreach (var candidate in candidates)
        {
            var character = candidate[0];
            var counts = lines
                .Where(line => line.Trim().Length > 0)
                .Select(line => line.Count(c => c == character))
                .Where(count => count > 0)
                .ToList();
            if (counts.Count == 0)
            {
                continue;
            }

            // Favor the delimiter that appears both consistently (same count per line) and densely.
            var mode = counts
                .GroupBy(count => count)
                .OrderByDescending(group => group.Count())
                .ThenByDescending(group => group.Key)
                .First();
            var score = mode.Count() * mode.Key;
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static string StripDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[^0-9.\-+]")]
    private static partial Regex AmountCleanupRegex();

    [GeneratedRegex(@"\d+[.,]\d+")]
    private static partial Regex AmountProbeRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
