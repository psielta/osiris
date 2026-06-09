using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Bills.Commands.CreateBill;
using Osiris.Application.Features.Bills.Commands.DeleteBill;
using Osiris.Application.Features.Bills.Commands.MarkBillAsPaid;
using Osiris.Application.Features.Bills.Commands.MarkBillAsPending;
using Osiris.Application.Features.Bills.Commands.UpdateBill;
using Osiris.Application.Features.Bills.Queries.GetBillDetails;
using Osiris.Application.Features.Bills.Queries.GetBillForEdit;
using Osiris.Application.Features.Bills.Queries.ListBills;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;
using Osiris.Domain.Enums;
using Osiris.Web.Helpers;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

[Authorize]
[Route("bills")]
public sealed class BillsController : AppController
{
    private const string PaymentPrefix = "Payment";
    private const string ErrorMessageKey = "BillsErrorMessage";

    private readonly IMediator _mediator;

    public BillsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? month, int? year, CancellationToken cancellationToken)
    {
        var today = BrazilDates.Today();
        var selectedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
        var selectedYear = year is >= 1 and <= 9999 ? year.Value : today.Year;

        var bills = await _mediator.Send(new ListBillsQuery(selectedYear, selectedMonth), cancellationToken);

        return View(new BillsIndexViewModel
        {
            Year = selectedYear,
            Month = selectedMonth,
            Bills = bills
        });
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new BillFormViewModel { DueDate = BrazilDates.Today() };
        await PopulateOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateBillCommand(
                    model.Description,
                    model.Amount,
                    model.DueDate,
                    model.CategoryId,
                    model.PaymentAccountId,
                    model.Notes),
                cancellationToken);

            if (result.IsSuccess)
            {
                return RedirectToIndexFor(model.DueDate);
            }

            AddResultErrors(result);
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
        }

        await PopulateOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var bill = await _mediator.Send(new GetBillDetailsQuery(id), cancellationToken);
        if (bill is null)
        {
            return NotFound();
        }

        var payment = new BillPayFormViewModel
        {
            PaidAt = BrazilDates.Today(),
            PaymentAccountId = bill.PaymentAccountId
        };

        return View(new BillDetailsViewModel
        {
            Bill = bill,
            Payment = payment,
            AccountOptions = await BuildAccountOptionsAsync(cancellationToken)
        });
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var bill = await _mediator.Send(new GetBillForEditQuery(id), cancellationToken);
        if (bill is null)
        {
            return NotFound();
        }

        var model = new BillFormViewModel
        {
            Id = bill.Id,
            Description = bill.Description,
            Amount = bill.Amount,
            DueDate = bill.DueDate,
            CategoryId = bill.CategoryId,
            PaymentAccountId = bill.PaymentAccountId,
            Notes = bill.Notes,
            IsPaid = bill.IsPaid
        };

        await PopulateOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BillFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;

        try
        {
            var result = await _mediator.Send(
                new UpdateBillCommand(
                    id,
                    model.Description,
                    model.Amount,
                    model.DueDate,
                    model.CategoryId,
                    model.PaymentAccountId,
                    model.Notes),
                cancellationToken);

            if (result.IsSuccess)
            {
                return RedirectToAction(nameof(Details), new { id });
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

        var current = await _mediator.Send(new GetBillForEditQuery(id), cancellationToken);
        model.IsPaid = current?.IsPaid ?? false;
        await PopulateOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var bill = await _mediator.Send(new GetBillDetailsQuery(id), cancellationToken);
        if (bill is null)
        {
            return NotFound();
        }

        return View(bill);
    }

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteBillCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/pay")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(
        Guid id,
        BillPayFormViewModel payment,
        [FromForm] string? returnTo,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new MarkBillAsPaidCommand(id, payment.PaidAt ?? BrazilDates.Today(), payment.PaymentAccountId),
                cancellationToken);

            if (result.IsSuccess)
            {
                return RedirectAfterStatusChange(id, returnTo);
            }

            if (result.Errors.Any(error => error.Code == ResultErrorCodes.NotFound))
            {
                return NotFound();
            }

            if (returnTo == "index")
            {
                TempData[ErrorMessageKey] = result.Errors.First().Message;
                return RedirectToAction(nameof(Index));
            }

            AddPaymentResultErrors(result);
        }
        catch (ValidationException exception)
        {
            if (returnTo == "index")
            {
                TempData[ErrorMessageKey] = exception.Errors.First().ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            AddPaymentValidationErrors(exception);
        }

        var model = await BuildDetailsViewModelAsync(id, payment, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(nameof(Details), model);
    }

    [HttpPost("{id:guid}/pending")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pending(
        Guid id,
        [FromForm] string? returnTo,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkBillAsPendingCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Errors.Any(error => error.Code == ResultErrorCodes.NotFound))
            {
                return NotFound();
            }

            TempData[ErrorMessageKey] = result.Errors.First().Message;
        }

        return RedirectAfterStatusChange(id, returnTo);
    }

    private IActionResult RedirectAfterStatusChange(Guid id, string? returnTo)
    {
        return returnTo == "index"
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult RedirectToIndexFor(DateOnly? dueDate)
    {
        if (dueDate is null)
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index), new { month = dueDate.Value.Month, year = dueDate.Value.Year });
    }

    private async Task<BillDetailsViewModel?> BuildDetailsViewModelAsync(
        Guid id,
        BillPayFormViewModel payment,
        CancellationToken cancellationToken)
    {
        var bill = await _mediator.Send(new GetBillDetailsQuery(id), cancellationToken);
        if (bill is null)
        {
            return null;
        }

        return new BillDetailsViewModel
        {
            Bill = bill,
            Payment = payment,
            AccountOptions = await BuildAccountOptionsAsync(cancellationToken)
        };
    }

    private async Task PopulateOptionsAsync(BillFormViewModel model, CancellationToken cancellationToken)
    {
        var categories = await _mediator.Send(new ListCategoriesQuery(IncludeArchived: false), cancellationToken);

        model.CategoryOptions = categories
            .Where(category => category.Type == CategoryType.Expense)
            .Select(category => new SelectListItem { Text = category.Name, Value = category.Id.ToString() })
            .ToArray();

        model.AccountOptions = await BuildAccountOptionsAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildAccountOptionsAsync(
        CancellationToken cancellationToken)
    {
        var accounts = await _mediator.Send(new ListFinancialAccountsQuery(IncludeArchived: false), cancellationToken);

        return accounts
            .Select(account => new SelectListItem { Text = account.Name, Value = account.Id.ToString() })
            .ToArray();
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
