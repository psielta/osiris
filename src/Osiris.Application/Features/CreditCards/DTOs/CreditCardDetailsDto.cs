namespace Osiris.Application.Features.CreditCards.DTOs;

public sealed record CreditCardDetailsDto(
    Guid Id,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    Guid? PaymentAccountId,
    string? PaymentAccountName,
    bool IsActive);
