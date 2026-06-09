using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCards.DTOs;

namespace Osiris.Application.Features.CreditCards.Queries.ListCreditCards;

public sealed class ListCreditCardsQueryHandler
    : IRequestHandler<ListCreditCardsQuery, IReadOnlyCollection<CreditCardListItemDto>>
{
    private readonly ICreditCardRepository _creditCards;
    private readonly ICurrentUser _currentUser;

    public ListCreditCardsQueryHandler(ICreditCardRepository creditCards, ICurrentUser currentUser)
    {
        _creditCards = creditCards;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<CreditCardListItemDto>> Handle(
        ListCreditCardsQuery request,
        CancellationToken cancellationToken)
    {
        var creditCards = await _creditCards.ListAsync(
            _currentUser.TenantId,
            request.IncludeArchived,
            cancellationToken);

        return creditCards
            .Select(card => new CreditCardListItemDto(
                card.Id,
                card.Name,
                card.Limit,
                card.ClosingDay,
                card.DueDay,
                card.IsActive))
            .ToArray();
    }
}
