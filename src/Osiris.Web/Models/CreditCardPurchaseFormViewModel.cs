using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Osiris.Web.Models;

public sealed class CreditCardPurchaseFormViewModel
{
    public Guid CardId { get; set; }

    public string CardName { get; set; } = string.Empty;

    [Display(Name = "Descrição")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Valor total")]
    public decimal? TotalAmount { get; set; }

    [Display(Name = "Data da compra")]
    [DataType(DataType.Date)]
    public DateOnly? PurchaseDate { get; set; }

    [Display(Name = "Parcelas")]
    public int? Installments { get; set; }

    [Display(Name = "Categoria")]
    public Guid? CategoryId { get; set; }

    [Display(Name = "Observações")]
    public string? Notes { get; set; }

    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();
}
