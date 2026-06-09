using Osiris.Application.Features.CreditCards.DTOs;
using Osiris.Application.Features.CreditCardStatements.DTOs;

namespace Osiris.Web.Models;

public sealed class CreditCardStatementsIndexViewModel
{
    public required CreditCardDetailsDto Card { get; init; }

    public IReadOnlyCollection<CreditCardStatementListItemDto> Statements { get; init; } =
        Array.Empty<CreditCardStatementListItemDto>();
}
