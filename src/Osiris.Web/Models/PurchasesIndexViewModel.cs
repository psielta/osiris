using Osiris.Application.Features.CreditCardPurchases.DTOs;

namespace Osiris.Web.Models;

public sealed class PurchasesIndexViewModel
{
    public DateRangeFilterViewModel Filter { get; init; } = new();

    public IReadOnlyCollection<CreditCardPurchaseOverviewDto> Purchases { get; init; } =
        Array.Empty<CreditCardPurchaseOverviewDto>();
}
