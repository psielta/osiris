using System.ComponentModel.DataAnnotations;
using Osiris.Domain.Enums;

namespace Osiris.Web.Models;

public sealed class OfxImportLineViewModel
{
    public string ExternalId { get; set; } = string.Empty;

    public DateOnly OccurredOn { get; set; }

    public decimal Amount { get; set; }

    public FinancialAccountMovementType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsDuplicate { get; set; }

    public bool IsInflow => Type == FinancialAccountMovementType.Income;

    [Display(Name = "Importar")]
    public bool Include { get; set; }

    [Display(Name = "Categoria")]
    public Guid? CategoryId { get; set; }
}
