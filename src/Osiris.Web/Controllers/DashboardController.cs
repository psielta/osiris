using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;
using Osiris.Web.Helpers;

namespace Osiris.Web.Controllers;

[Authorize]
[Route("dashboard")]
public sealed class DashboardController : Controller
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? month, int? year, CancellationToken cancellationToken)
    {
        var today = BrazilDates.Today();
        var selectedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
        var selectedYear = year is >= 1 and <= 9999 ? year.Value : today.Year;

        var summary = await _mediator.Send(
            new GetMonthlyDashboardSummaryQuery(selectedYear, selectedMonth),
            cancellationToken);

        return View(summary);
    }
}
