namespace Osiris.Domain.Services;

public sealed record CreditCardStatementCycle(
    int ReferenceMonth,
    int ReferenceYear,
    DateOnly ClosingDate,
    DateOnly DueDate);
