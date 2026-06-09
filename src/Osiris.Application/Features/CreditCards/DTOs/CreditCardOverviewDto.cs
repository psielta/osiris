using Osiris.Application.Features.CreditCardStatements.DTOs;

namespace Osiris.Application.Features.CreditCards.DTOs;

/// <summary>
/// Risk-focused numbers for one card: open balances consume the limit until paid, and the future
/// total covers installments in statements after the current cycle.
/// </summary>
public sealed record CreditCardOverviewDto(
    decimal UsedLimit,
    decimal AvailableLimit,
    decimal UsagePercentage,
    decimal FutureInstallmentsTotal,
    CreditCardStatementListItemDto? NextStatement);
