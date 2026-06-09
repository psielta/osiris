using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.CreditCardStatements.Queries.ListAllCreditCardStatements;

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
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var statements = await _mediator.Send(new ListAllCreditCardStatementsQuery(), cancellationToken);
        return View(statements);
    }
}
