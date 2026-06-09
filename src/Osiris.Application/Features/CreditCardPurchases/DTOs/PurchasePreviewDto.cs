namespace Osiris.Application.Features.CreditCardPurchases.DTOs;

public sealed record PurchasePreviewInstallmentDto(
    int Number,
    decimal Amount,
    int ReferenceMonth,
    int ReferenceYear,
    DateOnly DueDate);

/// <summary>
/// Pre-confirmation projection of an installment purchase: how the amount splits, which
/// statements receive each installment, and how the card limit looks after the purchase.
/// </summary>
public sealed record PurchasePreviewDto(
    IReadOnlyCollection<PurchasePreviewInstallmentDto> Installments,
    decimal TotalAmount,
    decimal Limit,
    decimal UsedLimit,
    decimal AvailableLimit,
    decimal ProjectedUsedLimit,
    decimal ProjectedUsagePercentage,
    bool HighLimitUsage,
    decimal ProjectedFutureInstallmentsTotal);
