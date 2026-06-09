namespace Osiris.Application.Common.Models;

public sealed record CreditCardStatementTotals(decimal InstallmentsTotal, decimal PaymentsTotal)
{
    public decimal OpenBalance => InstallmentsTotal - PaymentsTotal;
}
