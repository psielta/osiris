using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccounts.DTOs;

public sealed record FinancialAccountListItemDto(
    Guid Id,
    string Name,
    FinancialAccountType Type,
    decimal CurrentBalance,
    bool IsActive);
