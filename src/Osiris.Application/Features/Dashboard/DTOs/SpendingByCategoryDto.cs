namespace Osiris.Application.Features.Dashboard.DTOs;

/// <summary>
/// Spending view by category for one month. Card purchases count at purchase time, bills at due
/// date, and direct expenses are manual account movements. Statement payments never appear here:
/// they settle debt already counted as purchases.
/// </summary>
public sealed record SpendingByCategoryDto(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal CardPurchasesTotal,
    decimal BillsTotal,
    decimal DirectExpensesTotal)
{
    public decimal Total => CardPurchasesTotal + BillsTotal + DirectExpensesTotal;
}
