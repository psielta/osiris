using MediatR;
using Osiris.Application.Features.CreditCards.DTOs;

namespace Osiris.Application.Features.CreditCards.Queries.GetCreditCardForEdit;

public sealed record GetCreditCardForEditQuery(Guid Id) : IRequest<CreditCardEditDto?>;
