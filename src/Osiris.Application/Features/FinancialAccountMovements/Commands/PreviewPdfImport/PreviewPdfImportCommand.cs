using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewPdfImport;

/// <summary>
/// Sends a statement PDF to the AI extractor and returns the same preview shape as OFX/CSV
/// (transactions with duplicates flagged). Nothing is persisted until the user confirms via
/// <c>ImportOfxStatementCommand</c>.
/// </summary>
public sealed record PreviewPdfImportCommand(Guid AccountId, byte[] Content, string FileName)
    : IRequest<Result<OfxImportPreviewDto>>;
