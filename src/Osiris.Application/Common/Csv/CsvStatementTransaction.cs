using Osiris.Domain.Enums;

namespace Osiris.Application.Common.Csv;

/// <summary>
/// One transaction read from a CSV statement. <see cref="Amount"/> is always positive; the direction
/// is carried by <see cref="Type"/>. Mirrors the OFX parser's normalized output.
/// </summary>
public sealed record CsvStatementTransaction(
    string ExternalId,
    DateOnly OccurredOn,
    decimal Amount,
    FinancialAccountMovementType Type,
    string Description);
