namespace Osiris.Api.Contracts;

public sealed record CreateCreditCardRequest(
    string Name,
    decimal? Limit,
    int? ClosingDay,
    int? DueDay,
    Guid? PaymentAccountId);

public sealed record UpdateCreditCardRequest(
    string Name,
    decimal? Limit,
    int? ClosingDay,
    int? DueDay,
    Guid? PaymentAccountId);

public sealed record CreateCreditCardPurchaseRequest(
    Guid? CategoryId,
    string Description,
    decimal? TotalAmount,
    DateOnly? PurchaseDate,
    int? Installments,
    string? Notes);

public sealed record RegisterStatementPaymentRequest(
    decimal? Amount,
    DateOnly? PaidAt,
    Guid? FinancialAccountId,
    string? Notes);
