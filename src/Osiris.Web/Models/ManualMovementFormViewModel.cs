using System.ComponentModel.DataAnnotations;
using Osiris.Domain.Enums;

namespace Osiris.Web.Models;

public sealed class ManualMovementFormViewModel
{
    [Display(Name = "Tipo")]
    public FinancialAccountMovementType? Type { get; set; }

    [Display(Name = "Valor")]
    public decimal? Amount { get; set; }

    [Display(Name = "Data")]
    [DataType(DataType.Date)]
    public DateOnly? OccurredOn { get; set; }

    [Display(Name = "Descrição")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Categoria")]
    public Guid? CategoryId { get; set; }

    [Display(Name = "Observações")]
    public string? Notes { get; set; }
}
