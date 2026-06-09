using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.CreditCards.Queries.GetCreditCardDetails;
using Osiris.Application.Features.CreditCardStatementPayments.Commands.RegisterCreditCardStatementPayment;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Application.Features.CreditCardStatements.Queries.GetCreditCardStatementDetails;
using Osiris.Application.Features.CreditCardStatements.Queries.ListCreditCardStatements;
using Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;
using Osiris.Web.Helpers;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

[Authorize]
[Route("cards/{cardId:guid}/statements")]
public sealed class CreditCardStatementsController : AppController
{
    private const string PaymentPrefix = "Payment";

    private readonly IMediator _mediator;

    public CreditCardStatementsController(IMediator mediator)
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

        var statements = await _mediator.Send(new ListCreditCardStatementsQuery(cardId), cancellationToken);

        return View(new CreditCardStatementsIndexViewModel
        {
            Card = card,
            Statements = statements
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid cardId, Guid id, CancellationToken cancellationToken)
    {
        var statement = await GetStatementAsync(cardId, id, cancellationToken);
        if (statement is null)
        {
            return NotFound();
        }

        return View(statement);
    }

    [HttpGet("{id:guid}/pay")]
    public async Task<IActionResult> Pay(
        Guid cardId,
        Guid id,
        [FromQuery] bool full,
        CancellationToken cancellationToken)
    {
        var statement = await GetStatementAsync(cardId, id, cancellationToken);
        if (statement is null)
        {
            return NotFound();
        }

        var card = await _mediator.Send(new GetCreditCardDetailsQuery(cardId), cancellationToken);
        var payment = new StatementPaymentFormViewModel
        {
            Amount = full && statement.OpenBalance > 0m ? statement.OpenBalance : null,
            PaidAt = BrazilDates.Today(),
            FinancialAccountId = card?.PaymentAccountId
        };

        return View(await BuildPayViewModelAsync(statement, payment, cancellationToken));
    }

    [HttpPost("{id:guid}/pay")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(
        Guid cardId,
        Guid id,
        StatementPaymentFormViewModel payment,
        CancellationToken cancellationToken)
    {
        var statement = await GetStatementAsync(cardId, id, cancellationToken);
        if (statement is null)
        {
            return NotFound();
        }

        try
        {
            var result = await _mediator.Send(
                new RegisterCreditCardStatementPaymentCommand(
                    id,
                    payment.Amount,
                    payment.PaidAt,
                    payment.FinancialAccountId,
                    payment.Notes),
                cancellationToken);

            if (result.IsSuccess)
            {
                return RedirectToAction(nameof(Details), new { cardId, id });
            }

            if (result.Errors.Any(error => error.Code == ResultErrorCodes.NotFound))
            {
                return NotFound();
            }

            AddPaymentResultErrors(result);
        }
        catch (ValidationException exception)
        {
            AddPaymentValidationErrors(exception);
        }

        return View(await BuildPayViewModelAsync(statement, payment, cancellationToken));
    }

    private async Task<CreditCardStatementDetailsDto?> GetStatementAsync(
        Guid cardId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var statement = await _mediator.Send(new GetCreditCardStatementDetailsQuery(id), cancellationToken);
        if (statement is null || statement.CreditCardId != cardId)
        {
            return null;
        }

        return statement;
    }

    private async Task<CreditCardStatementPayViewModel> BuildPayViewModelAsync(
        CreditCardStatementDetailsDto statement,
        StatementPaymentFormViewModel payment,
        CancellationToken cancellationToken)
    {
        var accounts = await _mediator.Send(new ListFinancialAccountsQuery(IncludeArchived: false), cancellationToken);
        var accountOptions = accounts
            .Select(account => new SelectListItem { Text = account.Name, Value = account.Id.ToString() })
            .ToArray();

        return new CreditCardStatementPayViewModel
        {
            Statement = statement,
            Payment = payment,
            AccountOptions = accountOptions
        };
    }

    private void AddPaymentResultErrors(Result result)
    {
        foreach (var error in result.Errors)
        {
            var key = string.IsNullOrWhiteSpace(error.Field)
                ? string.Empty
                : $"{PaymentPrefix}.{NormalizeField(error.Field)}";
            ModelState.AddModelError(key, error.Message);
        }
    }

    private void AddPaymentValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(
                $"{PaymentPrefix}.{NormalizeField(error.PropertyName)}",
                error.ErrorMessage);
        }
    }
}
