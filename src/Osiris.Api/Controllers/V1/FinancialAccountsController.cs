using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Api.Contracts;
using Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;
using Osiris.Application.Features.FinancialAccounts.Commands.ArchiveFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Commands.CreateFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Commands.UpdateFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountForEdit;
using Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;

namespace Osiris.Api.Controllers.V1;

[Authorize]
[Route("api/v1/accounts")]
public sealed class FinancialAccountsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public FinancialAccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListFinancialAccountsQuery(IncludeArchived: true), cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new GetFinancialAccountForEditQuery(id), cancellationToken);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFinancialAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateFinancialAccountCommand(request.Name, request.Type, request.InitialBalance),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { id = result.Value })
            : Problem(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateFinancialAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateFinancialAccountCommand(id, request.Name, request.Type),
            cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ArchiveFinancialAccountCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpGet("{id:guid}/statement")]
    public async Task<IActionResult> Statement(Guid id, CancellationToken cancellationToken)
    {
        var statement = await _mediator.Send(new GetFinancialAccountDetailsQuery(id), cancellationToken);
        return statement is null ? NotFound() : Ok(statement);
    }

    [HttpPost("{id:guid}/movements")]
    public async Task<IActionResult> CreateMovement(Guid id, CreateMovementRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateManualMovementCommand(
                id,
                request.Type,
                request.Amount,
                request.OccurredOn,
                request.Description,
                request.CategoryId,
                request.Notes),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { id = result.Value })
            : Problem(result);
    }
}
