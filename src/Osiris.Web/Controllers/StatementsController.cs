using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.CreditCardStatements.Queries.ListAllCreditCardStatements;
using Osiris.Web.Helpers;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

/// <summary>
/// Tenant-wide statements screen; statement details remain under cards/{cardId}/statements.
/// </summary>
[Authorize]
[Route("statements")]
public sealed class StatementsController : AppController
{
    private readonly IMediator _mediator;

    public StatementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var filter = DateRangeFilterViewModel.FromQuery(BrazilDates.Today(), from, to);
        var statements = await _mediator.Send(
            new ListAllCreditCardStatementsQuery(filter.From, filter.To),
            cancellationToken);

        return View(new StatementsIndexViewModel
        {
            Filter = filter,
            Statements = statements
        });
    }
}
