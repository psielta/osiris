using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Dashboard.DTOs;

public sealed record CreditCardDashboardDto(
    Guid CreditCardId,
    string Name,
    decimal Limit,
    decimal UsedLimit,
    decimal AvailableLimit,
    decimal UsagePercentage,
    Guid? CurrentStatementId,
    decimal CurrentStatementTotal,
    decimal CurrentStatementPaidTotal,
    decimal CurrentStatementOpenBalance,
    DateOnly? CurrentStatementDueDate,
    CreditCardStatementStatus? CurrentStatementStatus,
    decimal FutureInstallmentsTotal);
