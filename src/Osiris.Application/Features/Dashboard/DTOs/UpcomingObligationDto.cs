namespace Osiris.Application.Features.Dashboard.DTOs;

public enum UpcomingObligationKind
{
    Bill = 1,
    CreditCardStatement = 2
}

public sealed record UpcomingObligationDto(
    UpcomingObligationKind Kind,
    Guid Id,
    Guid? CreditCardId,
    string Description,
    DateOnly DueDate,
    decimal Amount,
    bool IsOverdue);
