using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.Reports.DTOs;
using Osiris.Application.Features.Reports.Queries.ExportCashFlowReportPdf;

namespace Osiris.Api.Controllers.V1;

[Authorize]
[Route("api/v1/reports")]
public sealed class ReportsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("cash-flow/synthetic/pdf")]
    public Task<IActionResult> ExportSyntheticCashFlowPdf(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken) =>
        ExportCashFlowPdf(month, year, CashFlowReportKind.Synthetic, cancellationToken);

    [HttpGet("cash-flow/analytic/pdf")]
    public Task<IActionResult> ExportAnalyticCashFlowPdf(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken) =>
        ExportCashFlowPdf(month, year, CashFlowReportKind.Analytic, cancellationToken);

    private async Task<IActionResult> ExportCashFlowPdf(
        int? month,
        int? year,
        CashFlowReportKind kind,
        CancellationToken cancellationToken)
    {
        var (selectedYear, selectedMonth) = ResolveMonthYear(month, year);
        var file = await _mediator.Send(
            new ExportCashFlowReportPdfQuery(selectedYear, selectedMonth, kind),
            cancellationToken);

        return File(file.Content, file.ContentType, file.FileName);
    }

    private static (int Year, int Month) ResolveMonthYear(int? month, int? year)
    {
        var today = ApiDateDefaults.TodayInSaoPaulo();
        var selectedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
        var selectedYear = year is >= 1 and <= 9999 ? year.Value : today.Year;
        return (selectedYear, selectedMonth);
    }
}
