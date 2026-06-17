using MediatR;
using Osiris.Application.Features.CreditCardPurchases.DTOs;

namespace Osiris.Application.Features.CreditCardPurchases.Queries.ListAllCreditCardPurchases;

public sealed record ListAllCreditCardPurchasesQuery(
    DateOnly? From = null,
    DateOnly? To = null)
    : IRequest<IReadOnlyCollection<CreditCardPurchaseOverviewDto>>;
