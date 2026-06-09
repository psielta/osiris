using MediatR;
using Osiris.Application.Features.CreditCardPurchases.DTOs;

namespace Osiris.Application.Features.CreditCardPurchases.Queries.ListAllCreditCardPurchases;

public sealed record ListAllCreditCardPurchasesQuery
    : IRequest<IReadOnlyCollection<CreditCardPurchaseOverviewDto>>;
