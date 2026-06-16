using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Api.Contracts;
using Osiris.Application.Features.Categories.Commands.ArchiveCategory;
using Osiris.Application.Features.Categories.Commands.CreateCategory;
using Osiris.Application.Features.Categories.Commands.DeleteCategory;
using Osiris.Application.Features.Categories.Commands.UpdateCategory;
using Osiris.Application.Features.Categories.Queries.GetCategoryForEdit;
using Osiris.Application.Features.Categories.Queries.ListCategories;

namespace Osiris.Api.Controllers.V1;

[Authorize]
[Route("api/v1/categories")]
public sealed class CategoriesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListCategoriesQuery(IncludeArchived: true), cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var category = await _mediator.Send(new GetCategoryForEditQuery(id), cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCategoryCommand(request.Name, request.Type, request.Color),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { id = result.Value })
            : Problem(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCategoryCommand(id, request.Name, request.Type, request.Color),
            cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ArchiveCategoryCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result);
    }
}
