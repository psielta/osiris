using Osiris.Application.Features.Bills.DTOs;

namespace Osiris.Web.Models;

public sealed class BillsIndexViewModel
{
    public int Year { get; init; }

    public int Month { get; init; }

    public IReadOnlyCollection<BillListItemDto> Bills { get; init; } = Array.Empty<BillListItemDto>();
}
