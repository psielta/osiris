using System.ComponentModel.DataAnnotations;

namespace Osiris.Web.Models;

public sealed class BillPayFormViewModel
{
    [Display(Name = "Data do pagamento")]
    [DataType(DataType.Date)]
    public DateOnly? PaidAt { get; set; }

    [Display(Name = "Conta de pagamento (opcional)")]
    public Guid? PaymentAccountId { get; set; }
}
