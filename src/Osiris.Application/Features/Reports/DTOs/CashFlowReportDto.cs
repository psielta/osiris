using Osiris.Application.Features.Dashboard.DTOs;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Reports.DTOs;

public enum CashFlowReportKind
{
    Synthetic = 1,
    Analytic = 2
}

public sealed record CashFlowReportDto(
    CashFlowReportKind Kind,
    int Year,
    int Month,
    CashFlowSummaryDto CashFlow,
    IReadOnlyCollection<CashFlowReportAccountDto> Accounts,
    IReadOnlyCollection<CashFlowReportMovementDto> Movements,
    IReadOnlyCollection<CashFlowReportBillDto> Bills,
    IReadOnlyCollection<CashFlowReportStatementPaymentDto> StatementPayments,
    IReadOnlyCollection<CashFlowReportOpenStatementDto> OpenStatements);

public sealed record CashFlowReportAccountDto(
    Guid Id,
    string Name,
    FinancialAccountType Type,
    decimal CurrentBalance);

public sealed record CashFlowReportMovementDto(
    Guid Id,
    DateOnly OccurredOn,
    string FinancialAccountName,
    FinancialAccountMovementType Type,
    decimal Amount,
    bool IsInflow,
    string Description,
    string? CategoryName,
    string? Notes);

public sealed record CashFlowReportBillDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly DueDate,
    DateOnly? PaidAt,
    BillStatus Status,
    string? CategoryName,
    string? PaymentAccountName);

public sealed record CashFlowReportStatementPaymentDto(
    Guid Id,
    DateOnly PaidAt,
    string CreditCardName,
    int ReferenceMonth,
    int ReferenceYear,
    string? FinancialAccountName,
    decimal Amount,
    string? Notes);

public sealed record CashFlowReportOpenStatementDto(
    Guid Id,
    string CreditCardName,
    int ReferenceMonth,
    int ReferenceYear,
    DateOnly DueDate,
    CreditCardStatementStatus Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OpenBalance);
