using MediatR;
using Osiris.Application.Features.CreditCardStatements.DTOs;

namespace Osiris.Application.Features.CreditCardStatements.Queries.GetCreditCardStatementDetails;

public sealed record GetCreditCardStatementDetailsQuery(Guid Id) : IRequest<CreditCardStatementDetailsDto?>;
