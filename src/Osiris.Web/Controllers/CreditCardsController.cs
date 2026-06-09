using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.CreditCards.Commands.ArchiveCreditCard;
using Osiris.Application.Features.CreditCards.Commands.CreateCreditCard;
using Osiris.Application.Features.CreditCards.Commands.UpdateCreditCard;
using Osiris.Application.Features.CreditCardPurchases.Queries.ListCreditCardPurchases;
using Osiris.Application.Features.CreditCards.Queries.GetCreditCardDetails;
using Osiris.Application.Features.CreditCards.Queries.GetCreditCardForEdit;
using Osiris.Application.Features.CreditCards.Queries.ListCreditCards;
using Osiris.Application.Features.CreditCardStatements.Queries.GetCurrentCreditCardStatement;
using Osiris.Application.Features.CreditCardStatements.Queries.ListCreditCardStatements;
using Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

[Authorize]
[Route("cards")]
public sealed class CreditCardsController : AppController
{
    private readonly IMediator _mediator;

    public CreditCardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var cards = await _mediator.Send(new ListCreditCardsQuery(), cancellationToken);
        return View(cards);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new CreditCardFormViewModel
        {
            PaymentAccountOptions = await BuildPaymentAccountOptionsAsync(selectedId: null, cancellationToken)
        };

        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreditCardFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateCreditCardCommand(model.Name, model.Limit, model.ClosingDay, model.DueDay, model.PaymentAccountId),
                cancellationToken);

            if (result.IsFailure)
            {
                AddResultErrors(result);
                return await ViewWithOptionsAsync(model, cancellationToken);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return await ViewWithOptionsAsync(model, cancellationToken);
        }
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var card = await _mediator.Send(new GetCreditCardForEditQuery(id), cancellationToken);
        if (card is null)
        {
            return NotFound();
        }

        var model = new CreditCardFormViewModel
        {
            Id = card.Id,
            Name = card.Name,
            Limit = card.Limit,
            ClosingDay = card.ClosingDay,
            DueDay = card.DueDay,
            PaymentAccountId = card.PaymentAccountId,
            PaymentAccountOptions = await BuildPaymentAccountOptionsAsync(card.PaymentAccountId, cancellationToken)
        };

        return View(model);
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CreditCardFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;

        try
        {
            var result = await _mediator.Send(
                new UpdateCreditCardCommand(id, model.Name, model.Limit, model.ClosingDay, model.DueDay, model.PaymentAccountId),
                cancellationToken);

            if (result.IsSuccess)
            {
                return RedirectToAction(nameof(Index));
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

        return await ViewWithOptionsAsync(model, cancellationToken);
    }

    [HttpPost("{id:guid}/archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ArchiveCreditCardCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var card = await _mediator.Send(new GetCreditCardDetailsQuery(id), cancellationToken);
        if (card is null)
        {
            return NotFound();
        }

        var purchases = await _mediator.Send(new ListCreditCardPurchasesQuery(id), cancellationToken);
        var statements = await _mediator.Send(new ListCreditCardStatementsQuery(id), cancellationToken);
        var currentStatement = await _mediator.Send(new GetCurrentCreditCardStatementQuery(id), cancellationToken);

        return View(new CreditCardDetailsViewModel
        {
            Card = card,
            RecentPurchases = purchases.Take(5).ToArray(),
            TotalPurchases = purchases.Count,
            CurrentStatement = currentStatement,
            Statements = statements
        });
    }

    private async Task<IActionResult> ViewWithOptionsAsync(
        CreditCardFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.PaymentAccountOptions = await BuildPaymentAccountOptionsAsync(model.PaymentAccountId, cancellationToken);
        return View(model);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildPaymentAccountOptionsAsync(
        Guid? selectedId,
        CancellationToken cancellationToken)
    {
        var accounts = await _mediator.Send(new ListFinancialAccountsQuery(IncludeArchived: false), cancellationToken);
        var options = accounts
            .Select(account => new SelectListItem { Text = account.Name, Value = account.Id.ToString() })
            .ToList();

        // Preserve a currently-selected account that is no longer active (archived) so editing does not drop it.
        if (selectedId is not null && options.All(option => option.Value != selectedId.Value.ToString()))
        {
            var all = await _mediator.Send(new ListFinancialAccountsQuery(IncludeArchived: true), cancellationToken);
            var current = all.FirstOrDefault(account => account.Id == selectedId.Value);
            if (current is not null)
            {
                options.Insert(0, new SelectListItem { Text = current.Name, Value = current.Id.ToString() });
            }
        }

        return options;
    }
}
