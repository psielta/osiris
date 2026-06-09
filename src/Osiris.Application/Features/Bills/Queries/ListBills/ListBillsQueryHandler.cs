using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.Bills.DTOs;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.Bills.Queries.ListBills;

public sealed class ListBillsQueryHandler
    : IRequestHandler<ListBillsQuery, IReadOnlyCollection<BillListItemDto>>
{
    private readonly IBillRepository _bills;
    private readonly ICategoryRepository _categories;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ListBillsQueryHandler(
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

    public async Task<IReadOnlyCollection<BillListItemDto>> Handle(
        ListBillsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var bills = await _bills.ListByMonthAsync(tenantId, request.Year, request.Month, cancellationToken);
        var categories = await _categories.ListAsync(tenantId, includeArchived: true, cancellationToken);
        var accounts = await _accounts.ListAsync(tenantId, includeArchived: true, cancellationToken);

        var categoriesById = categories.ToDictionary(category => category.Id);
        var accountNames = accounts.ToDictionary(account => account.Id, account => account.Name);
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        return bills
            .Select(bill =>
            {
                var category = categoriesById.GetValueOrDefault(bill.CategoryId);
                return new BillListItemDto(
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
                    bill.PaymentAccountId is null
                        ? null
                        : accountNames.GetValueOrDefault(bill.PaymentAccountId.Value));
            })
            .ToArray();
    }
}
