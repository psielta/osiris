using MediatR;
using Osiris.Application.Features.CreditCardStatements.DTOs;

namespace Osiris.Application.Features.CreditCardStatements.Queries.GetCurrentCreditCardStatement;

/// <summary>
/// Returns the statement whose billing cycle contains today (the one new purchases enter),
/// or null when the card has no statement for the current cycle yet.
/// </summary>
public sealed record GetCurrentCreditCardStatementQuery(Guid CreditCardId)
    : IRequest<CreditCardStatementListItemDto?>;
