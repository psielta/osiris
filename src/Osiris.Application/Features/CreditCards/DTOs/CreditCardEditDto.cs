namespace Osiris.Application.Features.CreditCards.DTOs;

public sealed record CreditCardEditDto(
    Guid Id,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    Guid? PaymentAccountId);
