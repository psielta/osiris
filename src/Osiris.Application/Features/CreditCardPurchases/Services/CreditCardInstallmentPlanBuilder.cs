namespace Osiris.Application.Features.CreditCardPurchases.Services;

public static class CreditCardInstallmentPlanBuilder
{
    /// <summary>
    /// Splits a purchase total into installment amounts. Every installment receives the total
    /// divided by the count rounded down to cents, and the remainder cents go to the last
    /// installment so the sum always matches the total exactly.
    /// </summary>
    public static IReadOnlyList<decimal> SplitAmount(decimal totalAmount, int installments)
    {
        if (totalAmount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Total amount must be positive.");
        }

        if (installments < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(installments), "Installments must be greater than or equal to 1.");
        }

        var baseAmount = Math.Floor(totalAmount / installments * 100m) / 100m;
        var amounts = new decimal[installments];
        for (var index = 0; index < installments - 1; index++)
        {
            amounts[index] = baseAmount;
        }

        amounts[installments - 1] = totalAmount - (baseAmount * (installments - 1));

        return amounts;
    }
}
