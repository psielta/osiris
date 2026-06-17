using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Reports.DTOs;

namespace Osiris.Application.Features.Reports.Queries.ExportCashFlowReportPdf;

public sealed record ExportCashFlowReportPdfQuery(int Year, int Month, CashFlowReportKind Kind)
    : IRequest<FileExportResult>;
