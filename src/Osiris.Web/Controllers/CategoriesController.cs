using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.Categories.Commands.ArchiveCategory;
using Osiris.Application.Features.Categories.Commands.CreateCategory;
using Osiris.Application.Features.Categories.Commands.DeleteCategory;
using Osiris.Application.Features.Categories.Commands.UpdateCategory;
using Osiris.Application.Features.Categories.Queries.GetCategoryForEdit;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

[Authorize]
[Route("categories")]
public sealed class CategoriesController : AppController
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await _mediator.Send(new ListCategoriesQuery(), cancellationToken);
        return View(categories);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new CategoryFormViewModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateCategoryCommand(model.Name, model.Type, model.Color),
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
        var category = await _mediator.Send(new GetCategoryForEditQuery(id), cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return View(new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            Color = category.Color
        });
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;

        try
        {
            var result = await _mediator.Send(
                new UpdateCategoryCommand(id, model.Name, model.Type, model.Color),
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

    [HttpPost("{id:guid}/archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ArchiveCategoryCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var category = await _mediator.Send(new GetCategoryForEditQuery(id), cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}
