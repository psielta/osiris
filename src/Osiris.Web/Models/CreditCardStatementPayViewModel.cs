using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Application.Features.CreditCardStatements.DTOs;

namespace Osiris.Web.Models;

public sealed class CreditCardStatementPayViewModel
{
    public required CreditCardStatementDetailsDto Statement { get; init; }

    public StatementPaymentFormViewModel Payment { get; init; } = new();

    public IReadOnlyCollection<SelectListItem> AccountOptions { get; init; } = Array.Empty<SelectListItem>();
}
