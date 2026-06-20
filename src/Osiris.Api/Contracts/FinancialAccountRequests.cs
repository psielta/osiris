using Osiris.Domain.Enums;

namespace Osiris.Api.Contracts;

public sealed record CreateFinancialAccountRequest(string Name, FinancialAccountType? Type, decimal? InitialBalance);

public sealed record UpdateFinancialAccountRequest(string Name, FinancialAccountType? Type);

public sealed record CreateMovementRequest(
    FinancialAccountMovementType? Type,
    decimal? Amount,
    DateOnly? OccurredOn,
    string Description,
    Guid? CategoryId,
    string? Notes);

public sealed record ImportOfxStatementRequest(IReadOnlyList<ImportOfxLineRequest> Lines);

public sealed record ImportOfxLineRequest(
    string ExternalId,
    DateOnly OccurredOn,
    decimal Amount,
    FinancialAccountMovementType Type,
    string Description,
    Guid? CategoryId);
