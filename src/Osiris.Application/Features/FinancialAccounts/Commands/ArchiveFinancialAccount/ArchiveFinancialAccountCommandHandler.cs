using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.FinancialAccounts.Commands.ArchiveFinancialAccount;

public sealed class ArchiveFinancialAccountCommandHandler : IRequestHandler<ArchiveFinancialAccountCommand, Result>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveFinancialAccountCommandHandler(
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _accounts = accounts;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveFinancialAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(_currentUser.TenantId, request.Id, cancellationToken);
        if (account is null)
        {
            return Result.Failure(new ResultError("Conta não encontrada."));
        }

        account.Archive(_dateTimeProvider.UtcNow);
        await _accounts.UpdateAsync(account, cancellationToken);

        return Result.Success();
    }
}
