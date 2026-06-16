using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.FinancialAccounts.Queries.ExportFinancialAccountStatementPdf;

public sealed record ExportFinancialAccountStatementPdfQuery(Guid AccountId) : IRequest<FileExportResult?>;
