using Osiris.Domain.Enums;

namespace Osiris.Application.Common.Pdf;

/// <summary>
/// One transaction extracted from a PDF statement by the AI. <see cref="Amount"/> is always positive;
/// the direction is carried by <see cref="Type"/>. Mirrors the OFX/CSV parsers' normalized output.
/// </summary>
public sealed record ExtractedStatementTransaction(
    string ExternalId,
    DateOnly OccurredOn,
    decimal Amount,
    FinancialAccountMovementType Type,
    string Description);
