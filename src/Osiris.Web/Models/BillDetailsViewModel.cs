using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Application.Features.Bills.DTOs;

namespace Osiris.Web.Models;

public sealed class BillDetailsViewModel
{
    public required BillDetailsDto Bill { get; init; }

    public required BillPayFormViewModel Payment { get; init; }

    public IReadOnlyCollection<SelectListItem> AccountOptions { get; init; } = Array.Empty<SelectListItem>();
}
