using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Enums;

namespace Osiris.Application.Common.Ofx;

/// <summary>
/// Tolerant parser for OFX bank statements. Handles both OFX 1.x (SGML, frequently with unclosed
/// value tags and Windows-1252 encoding — the common shape for Brazilian banks) and OFX 2.x (XML).
/// It scans for the relevant tags rather than requiring well-formed markup, and never throws on
/// malformed input: anything it cannot read simply yields fewer (or zero) transactions.
/// </summary>
public sealed partial class OfxStatementParser : IOfxStatementParser
{
    private const int MaxDescriptionLength = 200;
    private const int MaxExternalIdLength = 255;
    private const string FallbackDescription = "Lançamento importado";

    static OfxStatementParser()
    {
        // Windows-1252 is not available out of the box on every runtime; register the provider so
        // Encoding.GetEncoding(1252) works (Brazilian OFX 1.x files are typically cp1252).
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public OfxStatement Parse(byte[] content)
    {
        if (content is null || content.Length == 0)
        {
            return Empty();
        }

        var text = Decode(content);
        var body = ExtractOfxBody(text);
        if (body is null)
        {
            return Empty();
        }

        var transactions = ParseTransactions(body);
        var (bankId, accountId) = ParseAccount(body);
        var transactionList = ExtractBlock(body, "BANKTRANLIST");
        var ledger = ExtractBlock(body, "LEDGERBAL");

        return new OfxStatement(
            BankId: bankId,
            AccountId: accountId,
            CurrencyCode: FirstLeaf(body, "CURDEF"),
            StartDate: ParseDate(FirstLeaf(transactionList, "DTSTART")),
            EndDate: ParseDate(FirstLeaf(transactionList, "DTEND")),
            LedgerBalance: ParseAmount(FirstLeaf(ledger, "BALAMT")),
            LedgerBalanceDate: ParseDate(FirstLeaf(ledger, "DTASOF")),
            Transactions: transactions);
    }

    private static OfxStatement Empty() => new(null, null, null, null, null, null, null, []);

    private static IReadOnlyList<OfxStatementTransaction> ParseTransactions(string body)
    {
        var transactions = new List<OfxStatementTransaction>();
        foreach (Match match in TransactionRegex().Matches(body))
        {
            var transaction = MapTransaction(match.Groups[1].Value);
            if (transaction is not null)
            {
                transactions.Add(transaction);
            }
        }

        return transactions;
    }

    private static OfxStatementTransaction? MapTransaction(string block)
    {
        var leaves = ReadLeaves(block);

        var amount = ParseAmount(leaves.GetValueOrDefault("TRNAMT"));
        if (amount is null || amount.Value == 0m)
        {
            return null;
        }

        var occurredOn = ParseDate(leaves.GetValueOrDefault("DTPOSTED"));
        if (occurredOn is null)
        {
            return null;
        }

        var type = amount.Value < 0m
            ? FinancialAccountMovementType.Expense
            : FinancialAccountMovementType.Income;

        var description = BuildDescription(leaves);
        var externalId = ResolveExternalId(leaves, occurredOn.Value, amount.Value, description);

        return new OfxStatementTransaction(externalId, occurredOn.Value, Math.Abs(amount.Value), type, description);
    }

    private static Dictionary<string, string> ReadLeaves(string block)
    {
        var leaves = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in LeafRegex().Matches(block))
        {
            var value = DecodeEntities(match.Groups["val"].Value).Trim();
            if (value.Length == 0)
            {
                continue;
            }

            leaves.TryAdd(match.Groups["tag"].Value, value);
        }

        return leaves;
    }

    private static string BuildDescription(Dictionary<string, string> leaves)
    {
        var raw = leaves.GetValueOrDefault("MEMO")
            ?? leaves.GetValueOrDefault("NAME")
            ?? FallbackDescription;

        var collapsed = WhitespaceRegex().Replace(raw, " ").Trim();
        if (collapsed.Length == 0)
        {
            collapsed = FallbackDescription;
        }

        return collapsed.Length > MaxDescriptionLength ? collapsed[..MaxDescriptionLength] : collapsed;
    }

    private static string ResolveExternalId(
        Dictionary<string, string> leaves,
        DateOnly occurredOn,
        decimal signedAmount,
        string description)
    {
        var fitid = leaves.GetValueOrDefault("FITID");
        if (!string.IsNullOrWhiteSpace(fitid))
        {
            var trimmed = fitid.Trim();
            return trimmed.Length > MaxExternalIdLength ? trimmed[..MaxExternalIdLength] : trimmed;
        }

        // Bank omitted FITID: synthesize a stable key so re-importing the same statement still de-dupes.
        var seed = $"{occurredOn:yyyyMMdd}|{signedAmount.ToString(CultureInfo.InvariantCulture)}|{description}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed)));
        return $"ofx-syn:{hash[..32]}";
    }

    private static (string? BankId, string? AccountId) ParseAccount(string body)
    {
        var block = ExtractBlock(body, "BANKACCTFROM") ?? ExtractBlock(body, "CCACCTFROM");
        return block is null ? (null, null) : (FirstLeaf(block, "BANKID"), FirstLeaf(block, "ACCTID"));
    }

    private static string Decode(byte[] content)
    {
        var start = content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF
            ? 3
            : 0;

        // The OFX header (1.x KEY:VALUE lines or the 2.x XML declaration) is ASCII, so a Latin1 pass
        // over the first bytes is always safe for sniffing the declared encoding.
        var probe = Encoding.Latin1.GetString(content, start, Math.Min(content.Length - start, 1024));
        return ResolveEncoding(probe).GetString(content, start, content.Length - start);
    }

    private static Encoding ResolveEncoding(string header)
    {
        if (header.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            var xml = XmlEncodingRegex().Match(header);
            return xml.Success ? EncodingFromName(xml.Groups["enc"].Value) : Encoding.UTF8;
        }

        var encoding = EncodingHeaderRegex().Match(header);
        if (!encoding.Success)
        {
            return Windows1252();
        }

        if (encoding.Groups["v"].Value.Contains("UTF", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.UTF8;
        }

        var charset = CharsetHeaderRegex().Match(header);
        return charset.Success ? EncodingFromName(charset.Groups["v"].Value) : Windows1252();
    }

    private static Encoding EncodingFromName(string name)
    {
        var normalized = name.Trim();
        if (normalized.Length == 0 || normalized.Equals("NONE", StringComparison.OrdinalIgnoreCase))
        {
            return Windows1252();
        }

        if (normalized.Contains("UTF", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.UTF8;
        }

        if (normalized.Contains("1252", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("ASCII", StringComparison.OrdinalIgnoreCase))
        {
            return Windows1252();
        }

        if (normalized.Contains("8859", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.Latin1;
        }

        try
        {
            return Encoding.GetEncoding(normalized);
        }
        catch (ArgumentException)
        {
            return Windows1252();
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

    private static string? ExtractOfxBody(string text)
    {
        var index = text.IndexOf("<OFX>", StringComparison.OrdinalIgnoreCase);
        return index < 0 ? null : text[index..];
    }

    private static string? ExtractBlock(string? text, string tag)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var escaped = Regex.Escape(tag);
        var match = Regex.Match(
            text,
            $"<{escaped}>(.*?)</{escaped}>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? FirstLeaf(string? text, string tag)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var match = Regex.Match(text, $"<{Regex.Escape(tag)}>([^<]*)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        var value = DecodeEntities(match.Groups[1].Value).Trim();
        return value.Length == 0 ? null : value;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
        {
            return null;
        }

        return DateOnly.TryParseExact(
            value.Trim()[..8],
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    private static decimal? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var raw = value.Trim();
        var hasDot = raw.Contains('.');
        var hasComma = raw.Contains(',');

        if (hasDot && hasComma)
        {
            // Rightmost separator is the decimal one; the other is a thousands grouping.
            raw = raw.LastIndexOf(',') > raw.LastIndexOf('.')
                ? raw.Replace(".", string.Empty).Replace(',', '.')
                : raw.Replace(",", string.Empty);
        }
        else if (hasComma)
        {
            raw = raw.Replace(',', '.');
        }

        return decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }

    private static string DecodeEntities(string value)
    {
        if (!value.Contains('&'))
        {
            return value;
        }

        value = value
            .Replace("&lt;", "<", StringComparison.Ordinal)
            .Replace("&gt;", ">", StringComparison.Ordinal)
            .Replace("&quot;", "\"", StringComparison.Ordinal)
            .Replace("&apos;", "'", StringComparison.Ordinal);

        value = NumericEntityRegex().Replace(value, static match =>
        {
            try
            {
                var group = match.Groups[1].Value;
                var code = group[0] is 'x' or 'X'
                    ? Convert.ToInt32(group[1..], 16)
                    : int.Parse(group, CultureInfo.InvariantCulture);
                return char.ConvertFromUtf32(code);
            }
            catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentOutOfRangeException)
            {
                return match.Value;
            }
        });

        return value.Replace("&amp;", "&", StringComparison.Ordinal);
    }

    [GeneratedRegex("<STMTTRN>(.*?)</STMTTRN>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TransactionRegex();

    [GeneratedRegex("<(?<tag>[A-Za-z0-9._]+)>(?<val>[^<]*)")]
    private static partial Regex LeafRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("&#(x[0-9A-Fa-f]+|[0-9]+);")]
    private static partial Regex NumericEntityRegex();

    [GeneratedRegex("<\\?xml[^>]*encoding=\"(?<enc>[^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex XmlEncodingRegex();

    [GeneratedRegex(@"ENCODING:\s*(?<v>[^\r\n]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EncodingHeaderRegex();

    [GeneratedRegex(@"CHARSET:\s*(?<v>[^\r\n]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CharsetHeaderRegex();
}
