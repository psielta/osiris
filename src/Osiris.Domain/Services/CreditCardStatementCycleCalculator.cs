namespace Osiris.Domain.Services;

/// <summary>
/// Resolves which credit card statement a purchase belongs to and the statement's
/// closing and due dates, given the card's configured closing and due days.
/// </summary>
public static class CreditCardStatementCycleCalculator
{
    /// <summary>
    /// Resolves the statement cycle for a purchase. Purchases made up to and including the
    /// closing date enter the statement of that month; purchases made after the closing date
    /// enter the next month's statement.
    /// </summary>
    public static CreditCardStatementCycle CalculateForPurchase(DateOnly purchaseDate, int closingDay, int dueDay)
    {
        EnsureValidDay(closingDay, nameof(closingDay));
        EnsureValidDay(dueDay, nameof(dueDay));

        var referenceYear = purchaseDate.Year;
        var referenceMonth = purchaseDate.Month;
        var closingDate = LastValidDayOfMonth(referenceYear, referenceMonth, closingDay);
        if (purchaseDate > closingDate)
        {
            (referenceYear, referenceMonth) = NextMonth(referenceYear, referenceMonth);
        }

        return CalculateForReference(referenceYear, referenceMonth, closingDay, dueDay);
    }

    /// <summary>
    /// Resolves the cycle for a known reference month/year, used to project the statements of
    /// subsequent installments. When the due day is greater than the closing day the statement
    /// is due in the same month it closes; otherwise it is due in the following month.
    /// </summary>
    public static CreditCardStatementCycle CalculateForReference(
        int referenceYear,
        int referenceMonth,
        int closingDay,
        int dueDay)
    {
        EnsureValidDay(closingDay, nameof(closingDay));
        EnsureValidDay(dueDay, nameof(dueDay));
        EnsureValidMonth(referenceMonth);

        var closingDate = LastValidDayOfMonth(referenceYear, referenceMonth, closingDay);
        var (dueYear, dueMonth) = dueDay > closingDay
            ? (referenceYear, referenceMonth)
            : NextMonth(referenceYear, referenceMonth);
        var dueDate = LastValidDayOfMonth(dueYear, dueMonth, dueDay);

        return new CreditCardStatementCycle(referenceMonth, referenceYear, closingDate, dueDate);
    }

    private static DateOnly LastValidDayOfMonth(int year, int month, int day)
    {
        return new DateOnly(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
    }

    private static (int Year, int Month) NextMonth(int year, int month)
    {
        return month == 12 ? (year + 1, 1) : (year, month + 1);
    }

    private static void EnsureValidDay(int day, string paramName)
    {
        if (day is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(paramName, "Day must be between 1 and 31.");
        }
    }

    private static void EnsureValidMonth(int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }
    }
}
