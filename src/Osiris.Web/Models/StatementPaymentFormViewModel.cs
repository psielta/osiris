using System.ComponentModel.DataAnnotations;

namespace Osiris.Web.Models;

public sealed class StatementPaymentFormViewModel
{
    [Display(Name = "Valor")]
    public decimal? Amount { get; set; }

    [Display(Name = "Data do pagamento")]
    [DataType(DataType.Date)]
    public DateOnly? PaidAt { get; set; }

    [Display(Name = "Conta de pagamento")]
    public Guid? FinancialAccountId { get; set; }

    [Display(Name = "Observações")]
    public string? Notes { get; set; }
}
