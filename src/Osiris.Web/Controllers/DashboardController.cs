using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.Dashboard.Queries.GetDashboard;

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
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dashboard = await _mediator.Send(new GetDashboardQuery(), cancellationToken);
        return View(dashboard);
    }
}
