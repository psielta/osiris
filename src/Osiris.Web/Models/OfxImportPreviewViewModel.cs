using Microsoft.AspNetCore.Mvc.Rendering;

namespace Osiris.Web.Models;

public sealed class OfxImportPreviewViewModel
{
    public Guid AccountId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string? BankId { get; set; }

    public string? AccountNumber { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public int TotalCount { get; set; }

    public int NewCount { get; set; }

    public int DuplicateCount { get; set; }

    public List<OfxImportLineViewModel> Lines { get; set; } = new();

    public IReadOnlyCollection<SelectListItem> Categories { get; set; } = Array.Empty<SelectListItem>();
}
