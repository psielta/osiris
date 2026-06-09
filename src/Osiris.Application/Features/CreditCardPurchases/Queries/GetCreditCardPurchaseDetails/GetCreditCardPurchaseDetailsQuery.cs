using MediatR;
using Osiris.Application.Features.CreditCardPurchases.DTOs;

namespace Osiris.Application.Features.CreditCardPurchases.Queries.GetCreditCardPurchaseDetails;

public sealed record GetCreditCardPurchaseDetailsQuery(Guid Id) : IRequest<CreditCardPurchaseDetailsDto?>;
