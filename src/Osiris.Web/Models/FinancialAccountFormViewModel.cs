using System.ComponentModel.DataAnnotations;
using Osiris.Domain.Enums;

namespace Osiris.Web.Models;

public sealed class FinancialAccountFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Tipo")]
    public FinancialAccountType? Type { get; set; }

    [Display(Name = "Saldo inicial")]
    public decimal? InitialBalance { get; set; }
}
