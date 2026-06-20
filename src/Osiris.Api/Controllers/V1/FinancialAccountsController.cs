using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Osiris.Api.Contracts;
using Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;
using Osiris.Application.Features.FinancialAccountMovements.Commands.ImportOfxStatement;
using Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewOfxImport;
using Osiris.Application.Features.FinancialAccounts.Commands.ArchiveFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Commands.CreateFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Commands.UpdateFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Queries.ExportFinancialAccountStatementPdf;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountForEdit;
using Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;

namespace Osiris.Api.Controllers.V1;

[Authorize]
[Route("api/v1/accounts")]
public sealed class FinancialAccountsController : ApiControllerBase
{
    private const long MaxOfxUploadBytes = 5 * 1024 * 1024;

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

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> ExportStatementPdf(Guid id, CancellationToken cancellationToken)
    {
        var file = await _mediator.Send(new ExportFinancialAccountStatementPdfQuery(id), cancellationToken);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
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

    [HttpPost("{id:guid}/movements/import/preview")]
    [RequestSizeLimit(MaxOfxUploadBytes)]
    public async Task<IActionResult> PreviewImport(Guid id, IFormFile? file, CancellationToken cancellationToken)
    {
        var content = await ReadAllBytesAsync(file, cancellationToken);

        var result = await _mediator.Send(
            new PreviewOfxImportCommand(id, content, file?.FileName ?? string.Empty),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    [HttpPost("{id:guid}/movements/import")]
    public async Task<IActionResult> Import(Guid id, ImportOfxStatementRequest request, CancellationToken cancellationToken)
    {
        var lines = (request.Lines ?? [])
            .Select(line => new ImportOfxLineInput(
                line.ExternalId,
                line.OccurredOn,
                line.Amount,
                line.Type,
                line.Description,
                line.CategoryId))
            .ToList();

        var result = await _mediator.Send(new ImportOfxStatementCommand(id, lines), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    private static async Task<byte[]> ReadAllBytesAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return [];
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }
}
