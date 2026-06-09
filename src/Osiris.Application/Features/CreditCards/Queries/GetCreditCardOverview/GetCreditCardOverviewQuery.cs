using MediatR;
using Osiris.Application.Features.CreditCards.DTOs;

namespace Osiris.Application.Features.CreditCards.Queries.GetCreditCardOverview;

public sealed record GetCreditCardOverviewQuery(Guid CreditCardId) : IRequest<CreditCardOverviewDto?>;
