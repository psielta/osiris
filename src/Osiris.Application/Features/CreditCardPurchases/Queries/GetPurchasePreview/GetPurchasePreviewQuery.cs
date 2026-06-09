using MediatR;
using Osiris.Application.Features.CreditCardPurchases.DTOs;

namespace Osiris.Application.Features.CreditCardPurchases.Queries.GetPurchasePreview;

/// <summary>
/// Returns null when the card does not belong to the tenant or the inputs are not previewable
/// yet (missing amount/date, out-of-range installments).
/// </summary>
public sealed record GetPurchasePreviewQuery(
    Guid CreditCardId,
    decimal? TotalAmount,
    DateOnly? PurchaseDate,
    int? Installments) : IRequest<PurchasePreviewDto?>;
