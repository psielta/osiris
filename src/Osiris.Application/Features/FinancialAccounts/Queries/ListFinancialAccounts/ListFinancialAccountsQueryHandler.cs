using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.FinancialAccounts.DTOs;

namespace Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;

public sealed class ListFinancialAccountsQueryHandler
    : IRequestHandler<ListFinancialAccountsQuery, IReadOnlyCollection<FinancialAccountListItemDto>>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;

    public ListFinancialAccountsQueryHandler(IFinancialAccountRepository accounts, ICurrentUser currentUser)
    {
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<FinancialAccountListItemDto>> Handle(
        ListFinancialAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await _accounts.ListAsync(
            _currentUser.TenantId,
            request.IncludeArchived,
            cancellationToken);

        return accounts
            .Select(account => new FinancialAccountListItemDto(
                account.Id,
                account.Name,
                account.Type,
                account.CurrentBalance,
                account.IsActive))
            .ToArray();
    }
}
