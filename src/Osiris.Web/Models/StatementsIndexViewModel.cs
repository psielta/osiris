using Osiris.Application.Features.CreditCardStatements.DTOs;

namespace Osiris.Web.Models;

public sealed class StatementsIndexViewModel
{
    public DateRangeFilterViewModel Filter { get; init; } = new();

    public IReadOnlyCollection<CreditCardStatementOverviewDto> Statements { get; init; } =
        Array.Empty<CreditCardStatementOverviewDto>();
}
