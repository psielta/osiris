using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.AnalyzeCsvImport;

/// <summary>
/// Inspects an uploaded CSV file (delimiter, encoding, sample rows) and returns the remembered
/// mapping for the account, so the user can map columns. Nothing is persisted.
/// </summary>
public sealed record AnalyzeCsvImportCommand(
    Guid AccountId,
    byte[] Content,
    string FileName,
    string? Delimiter = null,
    string? Encoding = null) : IRequest<Result<CsvAnalysisDto>>;
