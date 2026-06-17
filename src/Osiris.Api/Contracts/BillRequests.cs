namespace Osiris.Api.Contracts;

public sealed record CreateBillRequest(
    string Description,
    decimal? Amount,
    DateOnly? DueDate,
    Guid? CategoryId,
    Guid? PaymentAccountId,
    string? Notes);

public sealed record UpdateBillRequest(
    string Description,
    decimal? Amount,
    DateOnly? DueDate,
    Guid? CategoryId,
    Guid? PaymentAccountId,
    string? Notes);

public sealed record PayBillRequest(
    DateOnly? PaidAt,
    Guid? PaymentAccountId);
