namespace Osiris.Application.Features.Dashboard.DTOs;

/// <summary>
/// Cash view for one month: money that entered or left (or is expected to leave) the accounts.
/// Statement payments appear here as outflows even though they are not categorized expenses.
/// </summary>
public sealed record CashFlowSummaryDto(
    decimal IncomeTotal,
    decimal BillsPaidTotal,
    decimal StatementPaymentsTotal,
    decimal DirectExpensesTotal,
    decimal BillsOpenInMonthTotal,
    decimal StatementsOpenInMonthTotal,
    decimal TotalAccountsBalance,
    decimal ProjectedCashBalance);
