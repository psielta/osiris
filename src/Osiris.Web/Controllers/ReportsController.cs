using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.Reports.DTOs;
using Osiris.Application.Features.Reports.Queries.ExportCashFlowReportPdf;
using Osiris.Web.Helpers;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

[Authorize]
[Route("reports")]
public sealed class ReportsController : AppController
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public IActionResult Index(int? month, int? year)
    {
        var (selectedYear, selectedMonth) = ResolveMonthYear(month, year);
        return View(new ReportsIndexViewModel
        {
            Year = selectedYear,
            Month = selectedMonth
        });
    }

    [HttpGet("cash-flow/synthetic/pdf")]
    public Task<IActionResult> ExportSyntheticCashFlowPdf(
        int? month,
        int? year,
        CancellationToken cancellationToken) =>
        ExportCashFlowPdf(month, year, CashFlowReportKind.Synthetic, cancellationToken);

    [HttpGet("cash-flow/analytic/pdf")]
    public Task<IActionResult> ExportAnalyticCashFlowPdf(
        int? month,
        int? year,
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
        var today = BrazilDates.Today();
        var selectedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
        var selectedYear = year is >= 1 and <= 9999 ? year.Value : today.Year;
        return (selectedYear, selectedMonth);
    }
}
