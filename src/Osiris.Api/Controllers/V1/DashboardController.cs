using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;

namespace Osiris.Api.Controllers.V1;

[Authorize]
[Route("api/v1/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var today = ApiDateDefaults.TodayInSaoPaulo();
        var selectedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
        var selectedYear = year is >= 1 and <= 9999 ? year.Value : today.Year;

        var summary = await _mediator.Send(
            new GetMonthlyDashboardSummaryQuery(selectedYear, selectedMonth),
            cancellationToken);

        return Ok(summary);
    }
}
