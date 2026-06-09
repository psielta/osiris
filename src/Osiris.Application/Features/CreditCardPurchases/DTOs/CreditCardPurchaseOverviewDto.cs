namespace Osiris.Application.Features.CreditCardPurchases.DTOs;

/// <summary>
/// A purchase row in the tenant-wide purchases screen, carrying card and category names.
/// </summary>
public sealed record CreditCardPurchaseOverviewDto(
    Guid Id,
    Guid CreditCardId,
    string CreditCardName,
    string Description,
    string? CategoryName,
    decimal TotalAmount,
    DateOnly PurchaseDate,
    int Installments);
