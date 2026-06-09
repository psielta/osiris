using Osiris.Application.Features.CreditCardPurchases.DTOs;
using Osiris.Application.Features.CreditCards.DTOs;

namespace Osiris.Web.Models;

public sealed class CreditCardPurchasesIndexViewModel
{
    public required CreditCardDetailsDto Card { get; init; }

    public IReadOnlyCollection<CreditCardPurchaseListItemDto> Purchases { get; init; } =
        Array.Empty<CreditCardPurchaseListItemDto>();
}
