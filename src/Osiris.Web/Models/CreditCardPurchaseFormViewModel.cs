using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Osiris.Web.Models;

/// <summary>
/// How the value typed in the purchase form should be interpreted: as the full purchase total, or as the
/// value of a single installment (the form then multiplies by the installment count).
/// </summary>
public enum CreditCardAmountInputMode
{
    Total,
    PerInstallment
}

public sealed class CreditCardPurchaseFormViewModel
{
    public Guid CardId { get; set; }

    public string CardName { get; set; } = string.Empty;

    [Display(Name = "Descrição")]
    public string Description { get; set; } = string.Empty;

    public CreditCardAmountInputMode AmountMode { get; set; } = CreditCardAmountInputMode.Total;

    [Display(Name = "Valor")]
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
