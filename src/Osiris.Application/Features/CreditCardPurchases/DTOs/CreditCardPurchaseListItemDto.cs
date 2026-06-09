namespace Osiris.Application.Features.CreditCardPurchases.DTOs;

public sealed record CreditCardPurchaseListItemDto(
    Guid Id,
    string Description,
    string? CategoryName,
    decimal TotalAmount,
    DateOnly PurchaseDate,
    int Installments);
