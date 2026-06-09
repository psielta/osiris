using MediatR;
using Osiris.Application.Features.CreditCardStatements.DTOs;

namespace Osiris.Application.Features.CreditCardStatements.Queries.ListCreditCardStatements;

public sealed record ListCreditCardStatementsQuery(Guid CreditCardId)
    : IRequest<IReadOnlyCollection<CreditCardStatementListItemDto>>;
