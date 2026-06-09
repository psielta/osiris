using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.CreditCardPurchases.Queries.ListAllCreditCardPurchases;

namespace Osiris.Web.Controllers;

/// <summary>
/// Tenant-wide credit card purchases screen; purchase registration stays under each card.
/// </summary>
[Authorize]
[Route("purchases")]
public sealed class PurchasesController : AppController
{
    private readonly IMediator _mediator;

    public PurchasesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var purchases = await _mediator.Send(new ListAllCreditCardPurchasesQuery(), cancellationToken);
        return View(purchases);
    }
}
