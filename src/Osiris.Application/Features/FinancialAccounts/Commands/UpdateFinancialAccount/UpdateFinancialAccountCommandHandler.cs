using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.FinancialAccounts.Commands.UpdateFinancialAccount;

public sealed class UpdateFinancialAccountCommandHandler : IRequestHandler<UpdateFinancialAccountCommand, Result>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateFinancialAccountCommandHandler(
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _accounts = accounts;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateFinancialAccountCommand request, CancellationToken cancellationToken)
    {
        if (request.Type is null)
        {
            return Result.Failure(new ResultError("Selecione o tipo da conta.", nameof(request.Type)));
        }

        var tenantId = _currentUser.TenantId;
        var account = await _accounts.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (account is null)
        {
            return Result.Failure(new ResultError("Conta não encontrada.", Code: ResultErrorCodes.NotFound));
        }

        var normalizedName = FinancialAccount.NormalizeName(request.Name);
        var exists = await _accounts.ExistsAsync(tenantId, normalizedName, request.Id, cancellationToken);
        if (exists)
        {
            return Result.Failure(new ResultError("Já existe uma conta com este nome.", nameof(request.Name)));
        }

        account.Update(request.Name, request.Type.Value, _dateTimeProvider.UtcNow);
        await _accounts.UpdateAsync(account, cancellationToken);

        return Result.Success();
    }
}
