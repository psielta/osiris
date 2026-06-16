using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;
using Osiris.Application.Features.FinancialAccounts.Commands.ArchiveFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Commands.CreateFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Commands.UpdateFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Queries.ExportFinancialAccountStatementPdf;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountForEdit;
using Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;
using Osiris.Web.Helpers;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

[Authorize]
[Route("accounts")]
public sealed class FinancialAccountsController : AppController
{
    private const string MovementPrefix = "Movement";

    private readonly IMediator _mediator;

    public FinancialAccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accounts = await _mediator.Send(new ListFinancialAccountsQuery(), cancellationToken);
        return View(accounts);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new FinancialAccountFormViewModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FinancialAccountFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateFinancialAccountCommand(model.Name, model.Type, model.InitialBalance),
                cancellationToken);

            if (result.IsFailure)
            {
                AddResultErrors(result);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new GetFinancialAccountForEditQuery(id), cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        return View(new FinancialAccountFormViewModel
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            InitialBalance = account.InitialBalance
        });
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FinancialAccountFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;

        try
        {
            var result = await _mediator.Send(
                new UpdateFinancialAccountCommand(id, model.Name, model.Type),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Errors.Any(error => error.Code == ResultErrorCodes.NotFound))
                {
                    return NotFound();
                }

                AddResultErrors(result);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpPost("{id:guid}/archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ArchiveFinancialAccountCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var movement = new ManualMovementFormViewModel { OccurredOn = BrazilDates.Today() };
        var model = await BuildDetailsViewModelAsync(id, movement, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken cancellationToken)
    {
        var file = await _mediator.Send(new ExportFinancialAccountStatementPdfQuery(id), cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost("{id:guid}/movements")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMovement(
        Guid id,
        ManualMovementFormViewModel movement,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateManualMovementCommand(
                    id,
                    movement.Type,
                    movement.Amount,
                    movement.OccurredOn,
                    movement.Description,
                    movement.CategoryId,
                    movement.Notes),
                cancellationToken);

            if (result.IsSuccess)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            AddMovementResultErrors(result);
        }
        catch (ValidationException exception)
        {
            AddMovementValidationErrors(exception);
        }

        var model = await BuildDetailsViewModelAsync(id, movement, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(nameof(Details), model);
    }

    private async Task<FinancialAccountDetailsViewModel?> BuildDetailsViewModelAsync(
        Guid id,
        ManualMovementFormViewModel movement,
        CancellationToken cancellationToken)
    {
        var statement = await _mediator.Send(new GetFinancialAccountDetailsQuery(id), cancellationToken);
        if (statement is null)
        {
            return null;
        }

        var categories = await _mediator.Send(new ListCategoriesQuery(IncludeArchived: false), cancellationToken);
        var categoryItems = categories
            .Select(category => new SelectListItem
            {
                Text = category.Name,
                Value = category.Id.ToString()
            })
            .ToArray();

        return new FinancialAccountDetailsViewModel
        {
            Account = statement,
            Movement = movement,
            Categories = categoryItems
        };
    }

    private void AddMovementResultErrors(Result result)
    {
        foreach (var error in result.Errors)
        {
            var key = string.IsNullOrWhiteSpace(error.Field)
                ? string.Empty
                : $"{MovementPrefix}.{NormalizeField(error.Field)}";
            ModelState.AddModelError(key, error.Message);
        }
    }

    private void AddMovementValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(
                $"{MovementPrefix}.{NormalizeField(error.PropertyName)}",
                error.ErrorMessage);
        }
    }
}
