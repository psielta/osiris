namespace Osiris.Application.Common.Csv;

/// <summary>
/// How the value and direction (income/expense) of a CSV row are determined.
/// </summary>
public enum CsvAmountMode
{
    /// <summary>A single value column whose sign decides the direction (negative = expense).</summary>
    SignedAmount = 1,

    /// <summary>Separate credit and debit columns (credit = income, debit = expense).</summary>
    DebitCredit = 2,

    /// <summary>A positive value column plus a column whose text decides the direction.</summary>
    TypeColumn = 3
}
