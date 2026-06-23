namespace Osiris.Application.Features.CreditCardPurchases.DTOs;

public sealed record CreditCardPurchaseInstallmentDto(
    Guid Id,
    int InstallmentNumber,
    int TotalInstallments,
    decimal Amount,
    DateOnly DueDate,
    Guid CreditCardStatementId,
    int ReferenceMonth,
    int ReferenceYear);

public sealed record CreditCardPurchaseDetailsDto(
    Guid Id,
    Guid CreditCardId,
    string CreditCardName,
    string? CategoryName,
    Guid CategoryId,
    string Description,
    decimal TotalAmount,
    DateOnly PurchaseDate,
    int Installments,
    string? Notes,
    IReadOnlyCollection<CreditCardPurchaseInstallmentDto> InstallmentItems);
