using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.DTOs;

public sealed record MovementListItemDto(
    Guid Id,
    FinancialAccountMovementType Type,
    decimal Amount,
    bool IsInflow,
    DateOnly OccurredOn,
    string Description,
    Guid? CategoryId,
    string? Notes);
