using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Application.Features.CreditCardPurchases.Commands.CreateCreditCardPurchase;
using Osiris.Application.Features.CreditCardPurchases.Commands.DeleteCreditCardPurchase;
using Osiris.Application.Features.CreditCardPurchases.Queries.GetCreditCardPurchaseDetails;
using Osiris.Application.Features.CreditCardPurchases.Queries.ListCreditCardPurchases;
using Osiris.Application.Features.CreditCards.Queries.GetCreditCardDetails;
using Osiris.Domain.Enums;
using Osiris.Web.Helpers;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

[Authorize]
[Route("cards/{cardId:guid}/purchases")]
public sealed class CreditCardPurchasesController : AppController
{
    private readonly IMediator _mediator;

    public CreditCardPurchasesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid cardId, CancellationToken cancellationToken)
    {
        var card = await _mediator.Send(new GetCreditCardDetailsQuery(cardId), cancellationToken);
        if (card is null)
        {
            return NotFound();
        }

        var purchases = await _mediator.Send(new ListCreditCardPurchasesQuery(cardId), cancellationToken);

        return View(new CreditCardPurchasesIndexViewModel
        {
            Card = card,
            Purchases = purchases
        });
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(Guid cardId, CancellationToken cancellationToken)
    {
        var card = await _mediator.Send(new GetCreditCardDetailsQuery(cardId), cancellationToken);
        if (card is null)
        {
            return NotFound();
        }

        var model = new CreditCardPurchaseFormViewModel
        {
            CardId = card.Id,
            CardName = card.Name,
            PurchaseDate = BrazilDates.Today(),
            Installments = 1,
            CategoryOptions = await BuildCategoryOptionsAsync(cancellationToken)
        };

        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Guid cardId,
        CreditCardPurchaseFormViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateCreditCardPurchaseCommand(
                    cardId,
                    model.CategoryId,
                    model.Description,
                    model.TotalAmount,
                    model.PurchaseDate,
                    model.Installments,
                    model.Notes),
                cancellationToken);

            if (result.IsSuccess)
            {
                return RedirectToAction(nameof(Index), new { cardId });
            }

            if (result.Errors.Any(error => error.Code == ResultErrorCodes.NotFound))
            {
                return NotFound();
            }

            AddResultErrors(result);
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
        }

        return await ViewWithOptionsAsync(cardId, model, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid cardId, Guid id, CancellationToken cancellationToken)
    {
        var purchase = await _mediator.Send(new GetCreditCardPurchaseDetailsQuery(id), cancellationToken);
        if (purchase is null || purchase.CreditCardId != cardId)
        {
            return NotFound();
        }

        return View(purchase);
    }

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid cardId, Guid id, CancellationToken cancellationToken)
    {
        var purchase = await _mediator.Send(new GetCreditCardPurchaseDetailsQuery(id), cancellationToken);
        if (purchase is null || purchase.CreditCardId != cardId)
        {
            return NotFound();
        }

        var result = await _mediator.Send(new DeleteCreditCardPurchaseCommand(id), cancellationToken);
        if (result.IsSuccess)
        {
            return RedirectToAction(nameof(Index), new { cardId });
        }

        if (result.Errors.Any(error => error.Code == ResultErrorCodes.NotFound))
        {
            return NotFound();
        }

        AddResultErrors(result);
        return View(nameof(Details), purchase);
    }

    private async Task<IActionResult> ViewWithOptionsAsync(
        Guid cardId,
        CreditCardPurchaseFormViewModel model,
        CancellationToken cancellationToken)
    {
        var card = await _mediator.Send(new GetCreditCardDetailsQuery(cardId), cancellationToken);
        if (card is null)
        {
            return NotFound();
        }

        model.CardId = card.Id;
        model.CardName = card.Name;
        model.CategoryOptions = await BuildCategoryOptionsAsync(cancellationToken);

        return View(model);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildCategoryOptionsAsync(
        CancellationToken cancellationToken)
    {
        var categories = await _mediator.Send(new ListCategoriesQuery(IncludeArchived: false), cancellationToken);

        return categories
            .Where(category => category.Type == CategoryType.Expense)
            .Select(category => new SelectListItem { Text = category.Name, Value = category.Id.ToString() })
            .ToArray();
    }
}
