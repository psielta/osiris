namespace Osiris.Application.Features.Bills.DTOs;

public sealed record BillEditDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly DueDate,
    Guid CategoryId,
    Guid? PaymentAccountId,
    string? Notes,
    bool IsPaid);
