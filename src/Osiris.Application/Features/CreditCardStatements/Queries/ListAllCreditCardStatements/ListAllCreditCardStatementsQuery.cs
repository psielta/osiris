using MediatR;
using Osiris.Application.Features.CreditCardStatements.DTOs;

namespace Osiris.Application.Features.CreditCardStatements.Queries.ListAllCreditCardStatements;

public sealed record ListAllCreditCardStatementsQuery
    : IRequest<IReadOnlyCollection<CreditCardStatementOverviewDto>>;
