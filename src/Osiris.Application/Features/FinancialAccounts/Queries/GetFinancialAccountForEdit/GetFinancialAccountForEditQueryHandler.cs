using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.FinancialAccounts.DTOs;

namespace Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountForEdit;

public sealed class GetFinancialAccountForEditQueryHandler
    : IRequestHandler<GetFinancialAccountForEditQuery, FinancialAccountEditDto?>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;

    public GetFinancialAccountForEditQueryHandler(IFinancialAccountRepository accounts, ICurrentUser currentUser)
    {
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<FinancialAccountEditDto?> Handle(
        GetFinancialAccountForEditQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(_currentUser.TenantId, request.Id, cancellationToken);
        if (account is null)
        {
            return null;
        }

        return new FinancialAccountEditDto(account.Id, account.Name, account.Type, account.InitialBalance);
    }
}
