namespace Osiris.Domain.Enums;

public static class FinancialAccountMovementTypeExtensions
{
    public static bool IsInflow(this FinancialAccountMovementType type) => type switch
    {
        FinancialAccountMovementType.Income
            or FinancialAccountMovementType.TransferIn
            or FinancialAccountMovementType.Adjustment => true,
        _ => false
    };
}
