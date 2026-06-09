using MediatR;
using Osiris.Application.Features.CreditCards.DTOs;

namespace Osiris.Application.Features.CreditCards.Queries.GetCreditCardDetails;

public sealed record GetCreditCardDetailsQuery(Guid Id) : IRequest<CreditCardDetailsDto?>;
