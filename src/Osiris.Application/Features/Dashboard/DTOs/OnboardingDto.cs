namespace Osiris.Application.Features.Dashboard.DTOs;

/// <summary>
/// First-steps checklist shown on the dashboard while the tenant has no financial setup yet.
/// </summary>
public sealed record OnboardingDto(
    bool HasFinancialAccount,
    bool HasCreditCard,
    bool HasActiveCategories,
    bool HasFirstSpending)
{
    public bool IsComplete => HasFinancialAccount && HasCreditCard && HasActiveCategories && HasFirstSpending;
}
