using Osiris.Domain.Enums;

namespace Osiris.Application.Features.CreditCardStatements.DTOs;

public sealed record CreditCardStatementListItemDto(
    Guid Id,
    Guid CreditCardId,
    int ReferenceMonth,
    int ReferenceYear,
    DateOnly ClosingDate,
    DateOnly DueDate,
    CreditCardStatementStatus Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OpenBalance);
