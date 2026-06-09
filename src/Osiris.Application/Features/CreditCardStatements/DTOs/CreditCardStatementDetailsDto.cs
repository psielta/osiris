using Osiris.Domain.Enums;

namespace Osiris.Application.Features.CreditCardStatements.DTOs;

public sealed record CreditCardStatementInstallmentItemDto(
    Guid Id,
    Guid CreditCardPurchaseId,
    string PurchaseDescription,
    int InstallmentNumber,
    int TotalInstallments,
    decimal Amount);

public sealed record CreditCardStatementPaymentItemDto(
    Guid Id,
    decimal Amount,
    DateOnly PaidAt,
    Guid? FinancialAccountId,
    string? FinancialAccountName,
    string? Notes);

public sealed record CreditCardStatementDetailsDto(
    Guid Id,
    Guid CreditCardId,
    string CreditCardName,
    int ReferenceMonth,
    int ReferenceYear,
    DateOnly ClosingDate,
    DateOnly DueDate,
    CreditCardStatementStatus Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OpenBalance,
    IReadOnlyCollection<CreditCardStatementInstallmentItemDto> InstallmentItems,
    IReadOnlyCollection<CreditCardStatementPaymentItemDto> Payments);
