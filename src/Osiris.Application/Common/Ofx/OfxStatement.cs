using Osiris.Domain.Enums;

namespace Osiris.Application.Common.Ofx;

/// <summary>
/// Result of parsing an OFX bank statement file. Header fields are best-effort (banks vary widely)
/// and used only to enrich the import preview; the transactions are what gets imported.
/// </summary>
public sealed record OfxStatement(
    string? BankId,
    string? AccountId,
    string? CurrencyCode,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal? LedgerBalance,
    DateOnly? LedgerBalanceDate,
    IReadOnlyList<OfxStatementTransaction> Transactions);

/// <summary>
/// A single statement transaction (<c>STMTTRN</c>) already normalized to the domain shape:
/// <see cref="Amount"/> is always positive and <see cref="Type"/> carries the direction, so callers
/// never deal with a signed value. The signed OFX amount is converted exactly once, here in the parser.
/// </summary>
public sealed record OfxStatementTransaction(
    string ExternalId,
    DateOnly OccurredOn,
    decimal Amount,
    FinancialAccountMovementType Type,
    string Description);
