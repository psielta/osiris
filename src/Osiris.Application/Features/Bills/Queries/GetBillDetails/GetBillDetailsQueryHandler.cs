using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.Bills.DTOs;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.Bills.Queries.GetBillDetails;

public sealed class GetBillDetailsQueryHandler : IRequestHandler<GetBillDetailsQuery, BillDetailsDto?>
{
    private readonly IBillRepository _bills;
    private readonly ICategoryRepository _categories;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetBillDetailsQueryHandler(
        IBillRepository bills,
        ICategoryRepository categories,
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _bills = bills;
        _categories = categories;
        _accounts = accounts;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BillDetailsDto?> Handle(GetBillDetailsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var bill = await _bills.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (bill is null)
        {
            return null;
        }

        var category = await _categories.GetByIdAsync(tenantId, bill.CategoryId, cancellationToken);

        FinancialAccount? account = null;
        if (bill.PaymentAccountId is not null)
        {
            account = await _accounts.GetByIdAsync(tenantId, bill.PaymentAccountId.Value, cancellationToken);
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        return new BillDetailsDto(
            bill.Id,
            bill.Description,
            bill.Amount,
            bill.DueDate,
            bill.PaidAt,
            Bill.CalculateStatus(bill.PaidAt, bill.DueDate, today),
            bill.CategoryId,
            category?.Name,
            category?.Color,
            bill.PaymentAccountId,
            account?.Name,
            bill.Notes,
            bill.CreatedAtUtc,
            bill.UpdatedAtUtc);
    }
}
