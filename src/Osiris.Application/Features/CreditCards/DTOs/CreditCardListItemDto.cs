namespace Osiris.Application.Features.CreditCards.DTOs;

public sealed record CreditCardListItemDto(
    Guid Id,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    bool IsActive);
