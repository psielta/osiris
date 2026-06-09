using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.Bills.DTOs;

namespace Osiris.Application.Features.Bills.Queries.GetBillForEdit;

public sealed class GetBillForEditQueryHandler : IRequestHandler<GetBillForEditQuery, BillEditDto?>
{
    private readonly IBillRepository _bills;
    private readonly ICurrentUser _currentUser;

    public GetBillForEditQueryHandler(IBillRepository bills, ICurrentUser currentUser)
    {
        _bills = bills;
        _currentUser = currentUser;
    }

    public async Task<BillEditDto?> Handle(GetBillForEditQuery request, CancellationToken cancellationToken)
    {
        var bill = await _bills.GetByIdAsync(_currentUser.TenantId, request.Id, cancellationToken);
        if (bill is null)
        {
            return null;
        }

        return new BillEditDto(
            bill.Id,
            bill.Description,
            bill.Amount,
            bill.DueDate,
            bill.CategoryId,
            bill.PaymentAccountId,
            bill.Notes,
            bill.IsPaid);
    }
}
