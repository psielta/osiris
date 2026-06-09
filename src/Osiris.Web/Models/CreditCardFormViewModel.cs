using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Osiris.Web.Models;

public sealed class CreditCardFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Limite")]
    public decimal? Limit { get; set; }

    [Display(Name = "Dia de fechamento")]
    public int? ClosingDay { get; set; }

    [Display(Name = "Dia de vencimento")]
    public int? DueDay { get; set; }

    [Display(Name = "Conta de pagamento")]
    public Guid? PaymentAccountId { get; set; }

    public IReadOnlyCollection<SelectListItem> PaymentAccountOptions { get; set; } = Array.Empty<SelectListItem>();
}
