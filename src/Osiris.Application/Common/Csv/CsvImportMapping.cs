namespace Osiris.Application.Common.Csv;

/// <summary>
/// User-supplied mapping that tells the CSV parser how to read a particular bank's export: the
/// delimiter/encoding, which physical row holds the header, which columns carry each field, and the
/// date/number formats. Serialized as the remembered mapping for an account.
/// </summary>
public sealed record CsvImportMapping
{
    public string Delimiter { get; init; } = ";";

    public string Encoding { get; init; } = "utf-8";

    public bool HasHeader { get; init; } = true;

    /// <summary>Index (into the parsed rows) of the header row, or of the first data row when there is no header.</summary>
    public int HeaderLineIndex { get; init; }

    public CsvAmountMode AmountMode { get; init; } = CsvAmountMode.SignedAmount;

    public int DateColumn { get; init; }

    public int DescriptionColumn { get; init; }

    public int? SecondaryDescriptionColumn { get; init; }

    public int? AmountColumn { get; init; }

    public int? CreditColumn { get; init; }

    public int? DebitColumn { get; init; }

    public int? TypeColumn { get; init; }

    public int? ExternalIdColumn { get; init; }

    public IReadOnlyList<string> IncomeTokens { get; init; } = DefaultIncomeTokens;

    public IReadOnlyList<string> ExpenseTokens { get; init; } = DefaultExpenseTokens;

    public string DateFormat { get; init; } = "dd/MM/yyyy";

    public string DecimalSeparator { get; init; } = ",";

    // Matching strips accents and upper-cases, so the defaults are kept accent-free.
    public static readonly IReadOnlyList<string> DefaultIncomeTokens =
        new[] { "C", "CREDITO", "CREDIT", "ENTRADA", "RECEITA" };

    public static readonly IReadOnlyList<string> DefaultExpenseTokens =
        new[] { "D", "DEBITO", "DEBIT", "SAIDA", "DESPESA" };
}
