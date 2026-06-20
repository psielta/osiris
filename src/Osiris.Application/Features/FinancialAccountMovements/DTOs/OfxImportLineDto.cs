using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.DTOs;

/// <summary>
/// One transaction read from an OFX file, shown in the import preview. <see cref="Amount"/> is always
/// positive; <see cref="Type"/>/<see cref="IsInflow"/> carry the direction for display.
/// </summary>
public sealed record OfxImportLineDto(
    string RowKey,
    string ExternalId,
    DateOnly OccurredOn,
    decimal Amount,
    FinancialAccountMovementType Type,
    bool IsInflow,
    string Description,
    bool IsDuplicate);
