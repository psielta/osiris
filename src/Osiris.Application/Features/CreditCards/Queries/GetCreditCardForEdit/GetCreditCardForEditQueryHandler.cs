using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCards.DTOs;

namespace Osiris.Application.Features.CreditCards.Queries.GetCreditCardForEdit;

public sealed class GetCreditCardForEditQueryHandler
    : IRequestHandler<GetCreditCardForEditQuery, CreditCardEditDto?>
{
    private readonly ICreditCardRepository _creditCards;
    private readonly ICurrentUser _currentUser;

    public GetCreditCardForEditQueryHandler(ICreditCardRepository creditCards, ICurrentUser currentUser)
    {
        _creditCards = creditCards;
        _currentUser = currentUser;
    }

    public async Task<CreditCardEditDto?> Handle(
        GetCreditCardForEditQuery request,
        CancellationToken cancellationToken)
    {
        var card = await _creditCards.GetByIdAsync(_currentUser.TenantId, request.Id, cancellationToken);
        if (card is null)
        {
            return null;
        }

        return new CreditCardEditDto(
            card.Id,
            card.Name,
            card.Limit,
            card.ClosingDay,
            card.DueDay,
            card.PaymentAccountId);
    }
}
