using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Bills.DTOs;

public sealed record BillDetailsDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly DueDate,
    DateOnly? PaidAt,
    BillStatus Status,
    Guid CategoryId,
    string? CategoryName,
    string? CategoryColor,
    Guid? PaymentAccountId,
    string? PaymentAccountName,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
