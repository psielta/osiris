using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Api.Contracts;
using Osiris.Application.Features.Bills.Commands.CreateBill;
using Osiris.Application.Features.Bills.Commands.DeleteBill;
using Osiris.Application.Features.Bills.Commands.MarkBillAsPaid;
using Osiris.Application.Features.Bills.Commands.MarkBillAsPending;
using Osiris.Application.Features.Bills.Commands.UpdateBill;
using Osiris.Application.Features.Bills.Queries.GetBillDetails;
using Osiris.Application.Features.Bills.Queries.ListBills;

namespace Osiris.Api.Controllers.V1;

[Authorize]
[Route("api/v1/bills")]
public sealed class BillsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public BillsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var (selectedYear, selectedMonth) = ResolveMonth(year, month);
        var bills = await _mediator.Send(new ListBillsQuery(selectedYear, selectedMonth), cancellationToken);
        return Ok(bills);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var bill = await _mediator.Send(new GetBillDetailsQuery(id), cancellationToken);
        return bill is null ? NotFound() : Ok(bill);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBillRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateBillCommand(
                request.Description,
                request.Amount,
                request.DueDate,
                request.CategoryId,
                request.PaymentAccountId,
                request.Notes),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { id = result.Value })
            : Problem(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateBillRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateBillCommand(
                id,
                request.Description,
                request.Amount,
                request.DueDate,
                request.CategoryId,
                request.PaymentAccountId,
                request.Notes),
            cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteBillCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay(
        Guid id,
        PayBillRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new MarkBillAsPaidCommand(id, request.PaidAt, request.PaymentAccountId),
            cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result);
    }

    [HttpPost("{id:guid}/pending")]
    public async Task<IActionResult> Pending(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkBillAsPendingCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result);
    }

    private static (int Year, int Month) ResolveMonth(int? year, int? month)
    {
        var today = ApiDateDefaults.TodayInSaoPaulo();
        var selectedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
        var selectedYear = year is >= 1 and <= 9999 ? year.Value : today.Year;

        return (selectedYear, selectedMonth);
    }
}
