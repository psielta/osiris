using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccounts.DTOs;

public sealed record FinancialAccountEditDto(
    Guid Id,
    string Name,
    FinancialAccountType Type,
    decimal InitialBalance);
