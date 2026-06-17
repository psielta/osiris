using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.CreditCardPurchases.Queries.ListAllCreditCardPurchases;
using Osiris.Web.Helpers;
using Osiris.Web.Models;

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
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var filter = DateRangeFilterViewModel.FromQuery(BrazilDates.Today(), from, to);
        var purchases = await _mediator.Send(
            new ListAllCreditCardPurchasesQuery(filter.From, filter.To),
            cancellationToken);

        return View(new PurchasesIndexViewModel
        {
            Filter = filter,
            Purchases = purchases
        });
    }
}
