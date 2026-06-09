using Osiris.Application.Features.CreditCardPurchases.DTOs;
using Osiris.Application.Features.CreditCards.DTOs;
using Osiris.Application.Features.CreditCardStatements.DTOs;

namespace Osiris.Web.Models;

public sealed class CreditCardDetailsViewModel
{
    public required CreditCardDetailsDto Card { get; init; }

    public IReadOnlyCollection<CreditCardPurchaseListItemDto> RecentPurchases { get; init; } =
        Array.Empty<CreditCardPurchaseListItemDto>();

    public int TotalPurchases { get; init; }

    public CreditCardStatementListItemDto? CurrentStatement { get; init; }

    public CreditCardOverviewDto? Overview { get; init; }

    public IReadOnlyCollection<CreditCardStatementListItemDto> Statements { get; init; } =
        Array.Empty<CreditCardStatementListItemDto>();
}
