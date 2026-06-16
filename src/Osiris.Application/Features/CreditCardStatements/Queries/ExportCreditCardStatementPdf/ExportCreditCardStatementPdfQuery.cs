using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.CreditCardStatements.Queries.ExportCreditCardStatementPdf;

public sealed record ExportCreditCardStatementPdfQuery(Guid CreditCardId, Guid StatementId)
    : IRequest<FileExportResult?>;
