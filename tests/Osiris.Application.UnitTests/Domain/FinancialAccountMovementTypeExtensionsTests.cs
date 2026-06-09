using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Domain;

public sealed class FinancialAccountMovementTypeExtensionsTests
{
    [Theory]
    [InlineData(FinancialAccountMovementType.Income, true)]
    [InlineData(FinancialAccountMovementType.TransferIn, true)]
    [InlineData(FinancialAccountMovementType.Adjustment, true)]
    [InlineData(FinancialAccountMovementType.Expense, false)]
    [InlineData(FinancialAccountMovementType.BillPayment, false)]
    [InlineData(FinancialAccountMovementType.CreditCardStatementPayment, false)]
    [InlineData(FinancialAccountMovementType.TransferOut, false)]
    public void IsInflow_ShouldClassifyByType(FinancialAccountMovementType type, bool expected)
    {
        Assert.Equal(expected, type.IsInflow());
    }
}
