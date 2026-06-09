using Osiris.Domain.Enums;

namespace Osiris.Application.Features.CreditCardStatements.DTOs;

/// <summary>
/// A statement row in the tenant-wide statements screen, carrying the card name so the list can
/// span every credit card.
/// </summary>
public sealed record CreditCardStatementOverviewDto(
    Guid Id,
    Guid CreditCardId,
    string CreditCardName,
    int ReferenceMonth,
    int ReferenceYear,
    DateOnly ClosingDate,
    DateOnly DueDate,
    CreditCardStatementStatus Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OpenBalance);
