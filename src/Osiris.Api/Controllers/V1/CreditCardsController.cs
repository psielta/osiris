using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Api.Contracts;
using Osiris.Application.Features.CreditCards.Commands.ArchiveCreditCard;
using Osiris.Application.Features.CreditCards.Commands.CreateCreditCard;
using Osiris.Application.Features.CreditCards.Commands.UpdateCreditCard;
using Osiris.Application.Features.CreditCards.Queries.GetCreditCardDetails;
using Osiris.Application.Features.CreditCards.Queries.GetCreditCardOverview;
using Osiris.Application.Features.CreditCards.Queries.ListCreditCards;
using Osiris.Application.Features.CreditCardPurchases.Commands.CreateCreditCardPurchase;
using Osiris.Application.Features.CreditCardPurchases.Commands.DeleteCreditCardPurchase;
using Osiris.Application.Features.CreditCardPurchases.Queries.GetCreditCardPurchaseDetails;
using Osiris.Application.Features.CreditCardPurchases.Queries.GetPurchasePreview;
using Osiris.Application.Features.CreditCardPurchases.Queries.ListAllCreditCardPurchases;
using Osiris.Application.Features.CreditCardPurchases.Queries.ListCreditCardPurchases;
using Osiris.Application.Features.CreditCardStatementPayments.Commands.RegisterCreditCardStatementPayment;
using Osiris.Application.Features.CreditCardStatements.Queries.ExportCreditCardStatementPdf;
using Osiris.Application.Features.CreditCardStatements.Queries.GetCreditCardStatementDetails;
using Osiris.Application.Features.CreditCardStatements.Queries.GetCurrentCreditCardStatement;
using Osiris.Application.Features.CreditCardStatements.Queries.ListAllCreditCardStatements;
using Osiris.Application.Features.CreditCardStatements.Queries.ListCreditCardStatements;

namespace Osiris.Api.Controllers.V1;

[Authorize]
[Route("api/v1/cards")]
public sealed class CreditCardsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public CreditCardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListCreditCardsQuery(IncludeArchived: true), cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var card = await _mediator.Send(new GetCreditCardDetailsQuery(id), cancellationToken);
        return card is null ? NotFound() : Ok(card);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCreditCardCommand(
                request.Name,
                request.Limit,
                request.ClosingDay,
                request.DueDay,
                request.PaymentAccountId),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { id = result.Value })
            : Problem(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateCreditCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCreditCardCommand(
                id,
                request.Name,
                request.Limit,
                request.ClosingDay,
                request.DueDay,
                request.PaymentAccountId),
            cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ArchiveCreditCardCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpGet("{id:guid}/overview")]
    public async Task<IActionResult> Overview(Guid id, CancellationToken cancellationToken)
    {
        var overview = await _mediator.Send(new GetCreditCardOverviewQuery(id), cancellationToken);
        return overview is null ? NotFound() : Ok(overview);
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> ListAllPurchases(CancellationToken cancellationToken)
    {
        var purchases = await _mediator.Send(new ListAllCreditCardPurchasesQuery(), cancellationToken);
        return Ok(purchases);
    }

    [HttpGet("statements")]
    public async Task<IActionResult> ListAllStatements(CancellationToken cancellationToken)
    {
        var statements = await _mediator.Send(new ListAllCreditCardStatementsQuery(), cancellationToken);
        return Ok(statements);
    }

    [HttpGet("{cardId:guid}/purchases")]
    public async Task<IActionResult> ListPurchases(Guid cardId, CancellationToken cancellationToken)
    {
        if (!await CardExistsAsync(cardId, cancellationToken))
        {
            return NotFound();
        }

        var purchases = await _mediator.Send(new ListCreditCardPurchasesQuery(cardId), cancellationToken);
        return Ok(purchases);
    }

    [HttpGet("{cardId:guid}/purchases/preview")]
    public async Task<IActionResult> PreviewPurchase(
        Guid cardId,
        [FromQuery] decimal? totalAmount,
        [FromQuery] DateOnly? purchaseDate,
        [FromQuery] int? installments,
        CancellationToken cancellationToken)
    {
        if (!await CardExistsAsync(cardId, cancellationToken))
        {
            return NotFound();
        }

        var preview = await _mediator.Send(
            new GetPurchasePreviewQuery(cardId, totalAmount, purchaseDate, installments),
            cancellationToken);

        return Ok(preview);
    }

    [HttpPost("{cardId:guid}/purchases")]
    public async Task<IActionResult> CreatePurchase(
        Guid cardId,
        CreateCreditCardPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCreditCardPurchaseCommand(
                cardId,
                request.CategoryId,
                request.Description,
                request.TotalAmount,
                request.PurchaseDate,
                request.Installments,
                request.Notes),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { id = result.Value })
            : Problem(result);
    }

    [HttpGet("{cardId:guid}/purchases/{id:guid}")]
    public async Task<IActionResult> GetPurchase(Guid cardId, Guid id, CancellationToken cancellationToken)
    {
        var purchase = await _mediator.Send(new GetCreditCardPurchaseDetailsQuery(id), cancellationToken);
        return purchase is null || purchase.CreditCardId != cardId ? NotFound() : Ok(purchase);
    }

    [HttpDelete("{cardId:guid}/purchases/{id:guid}")]
    public async Task<IActionResult> DeletePurchase(Guid cardId, Guid id, CancellationToken cancellationToken)
    {
        var purchase = await _mediator.Send(new GetCreditCardPurchaseDetailsQuery(id), cancellationToken);
        if (purchase is null || purchase.CreditCardId != cardId)
        {
            return NotFound();
        }

        var result = await _mediator.Send(new DeleteCreditCardPurchaseCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpGet("{cardId:guid}/statements")]
    public async Task<IActionResult> ListStatements(Guid cardId, CancellationToken cancellationToken)
    {
        if (!await CardExistsAsync(cardId, cancellationToken))
        {
            return NotFound();
        }

        var statements = await _mediator.Send(new ListCreditCardStatementsQuery(cardId), cancellationToken);
        return Ok(statements);
    }

    [HttpGet("{cardId:guid}/statements/current")]
    public async Task<IActionResult> CurrentStatement(Guid cardId, CancellationToken cancellationToken)
    {
        if (!await CardExistsAsync(cardId, cancellationToken))
        {
            return NotFound();
        }

        var statement = await _mediator.Send(new GetCurrentCreditCardStatementQuery(cardId), cancellationToken);
        return Ok(statement);
    }

    [HttpGet("{cardId:guid}/statements/{id:guid}")]
    public async Task<IActionResult> GetStatement(Guid cardId, Guid id, CancellationToken cancellationToken)
    {
        var statement = await GetStatementForCardAsync(cardId, id, cancellationToken);
        return statement is null ? NotFound() : Ok(statement);
    }

    [HttpPost("{cardId:guid}/statements/{id:guid}/payments")]
    public async Task<IActionResult> RegisterPayment(
        Guid cardId,
        Guid id,
        RegisterStatementPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (await GetStatementForCardAsync(cardId, id, cancellationToken) is null)
        {
            return NotFound();
        }

        var result = await _mediator.Send(
            new RegisterCreditCardStatementPaymentCommand(
                id,
                request.Amount,
                request.PaidAt,
                request.FinancialAccountId,
                request.Notes),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { id = result.Value })
            : Problem(result);
    }

    [HttpGet("{cardId:guid}/statements/{id:guid}/pdf")]
    public async Task<IActionResult> ExportStatementPdf(Guid cardId, Guid id, CancellationToken cancellationToken)
    {
        var file = await _mediator.Send(new ExportCreditCardStatementPdfQuery(cardId, id), cancellationToken);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
    }

    private async Task<bool> CardExistsAsync(Guid cardId, CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetCreditCardDetailsQuery(cardId), cancellationToken) is not null;
    }

    private async Task<Osiris.Application.Features.CreditCardStatements.DTOs.CreditCardStatementDetailsDto?>
        GetStatementForCardAsync(Guid cardId, Guid id, CancellationToken cancellationToken)
    {
        var statement = await _mediator.Send(new GetCreditCardStatementDetailsQuery(id), cancellationToken);
        return statement is null || statement.CreditCardId != cardId ? null : statement;
    }
}
