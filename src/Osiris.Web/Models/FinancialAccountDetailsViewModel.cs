using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Application.Features.FinancialAccounts.DTOs;

namespace Osiris.Web.Models;

public sealed class FinancialAccountDetailsViewModel
{
    public required FinancialAccountStatementDto Account { get; init; }

    public ManualMovementFormViewModel Movement { get; init; } = new();

    public IReadOnlyCollection<SelectListItem> Categories { get; init; } = Array.Empty<SelectListItem>();
}
