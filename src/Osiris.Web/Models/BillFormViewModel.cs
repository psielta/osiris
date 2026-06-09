using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Osiris.Web.Models;

public sealed class BillFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Descrição")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Valor")]
    public decimal? Amount { get; set; }

    [Display(Name = "Vencimento")]
    [DataType(DataType.Date)]
    public DateOnly? DueDate { get; set; }

    [Display(Name = "Categoria")]
    public Guid? CategoryId { get; set; }

    [Display(Name = "Conta para pagamento (opcional)")]
    public Guid? PaymentAccountId { get; set; }

    [Display(Name = "Observações")]
    public string? Notes { get; set; }

    public bool IsPaid { get; set; }

    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> AccountOptions { get; set; } = Array.Empty<SelectListItem>();
}
