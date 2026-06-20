using MediatR;
using Osiris.Application.Common.Csv;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewCsvImport;

/// <summary>
/// Applies a column mapping to an uploaded CSV and returns the same preview shape as the OFX import
/// (transactions with duplicates flagged). Also remembers the mapping for the account. Nothing is
/// persisted as a movement until the user confirms via <c>ImportOfxStatementCommand</c>.
/// </summary>
public sealed record PreviewCsvImportCommand(
    Guid AccountId,
    byte[] Content,
    string FileName,
    CsvImportMapping Mapping) : IRequest<Result<OfxImportPreviewDto>>;
