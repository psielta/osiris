using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewOfxImport;

/// <summary>
/// Parses an uploaded OFX file and returns a preview (with duplicates flagged) without persisting anything.
/// </summary>
public sealed record PreviewOfxImportCommand(
    Guid AccountId,
    byte[] Content,
    string FileName) : IRequest<Result<OfxImportPreviewDto>>;
