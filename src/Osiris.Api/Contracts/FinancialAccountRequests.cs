using Osiris.Application.Common.Csv;
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

/// <summary>
/// CSV import preview request. <see cref="Content"/> is the Base64-encoded file (so the bytes and the
/// column mapping travel together as JSON); the decoded size is validated against the 5 MB limit.
/// </summary>
public sealed record PreviewCsvImportRequest(string? FileName, string? Content, CsvImportMapping? Mapping);
