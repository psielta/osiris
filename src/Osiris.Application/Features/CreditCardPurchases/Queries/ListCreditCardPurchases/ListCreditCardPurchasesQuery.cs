using MediatR;
using Osiris.Application.Features.CreditCardPurchases.DTOs;

namespace Osiris.Application.Features.CreditCardPurchases.Queries.ListCreditCardPurchases;

public sealed record ListCreditCardPurchasesQuery(Guid CreditCardId)
    : IRequest<IReadOnlyCollection<CreditCardPurchaseListItemDto>>;
