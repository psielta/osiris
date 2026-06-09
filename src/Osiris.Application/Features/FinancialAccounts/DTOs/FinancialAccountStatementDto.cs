using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccounts.DTOs;

public sealed record FinancialAccountStatementDto(
    Guid Id,
    string Name,
    FinancialAccountType Type,
    decimal InitialBalance,
    decimal CurrentBalance,
    bool IsActive,
    IReadOnlyCollection<MovementListItemDto> Movements);
