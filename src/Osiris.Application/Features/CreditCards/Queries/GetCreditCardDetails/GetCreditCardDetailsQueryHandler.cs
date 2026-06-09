using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCards.DTOs;

namespace Osiris.Application.Features.CreditCards.Queries.GetCreditCardDetails;

public sealed class GetCreditCardDetailsQueryHandler
    : IRequestHandler<GetCreditCardDetailsQuery, CreditCardDetailsDto?>
{
    private readonly ICreditCardRepository _creditCards;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;

    public GetCreditCardDetailsQueryHandler(
        ICreditCardRepository creditCards,
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser)
    {
        _creditCards = creditCards;
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<CreditCardDetailsDto?> Handle(
        GetCreditCardDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var card = await _creditCards.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (card is null)
        {
            return null;
        }

        string? paymentAccountName = null;
        if (card.PaymentAccountId is not null)
        {
            var account = await _accounts.GetByIdAsync(tenantId, card.PaymentAccountId.Value, cancellationToken);
            paymentAccountName = account?.Name;
        }

        return new CreditCardDetailsDto(
            card.Id,
            card.Name,
            card.Limit,
            card.ClosingDay,
            card.DueDay,
            card.PaymentAccountId,
            paymentAccountName,
            card.IsActive);
    }
}
