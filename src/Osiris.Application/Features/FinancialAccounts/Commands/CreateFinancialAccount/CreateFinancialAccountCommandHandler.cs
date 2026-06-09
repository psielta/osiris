using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.FinancialAccounts.Commands.CreateFinancialAccount;

public sealed class CreateFinancialAccountCommandHandler : IRequestHandler<CreateFinancialAccountCommand, Result<Guid>>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;

    public CreateFinancialAccountCommandHandler(IFinancialAccountRepository accounts, ICurrentUser currentUser)
    {
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateFinancialAccountCommand request, CancellationToken cancellationToken)
    {
        if (request.Type is null)
        {
            return Result<Guid>.Failure(new ResultError("Selecione o tipo da conta.", nameof(request.Type)));
        }

        if (request.InitialBalance is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe o saldo inicial.", nameof(request.InitialBalance)));
        }

        var tenantId = _currentUser.TenantId;
        var normalizedName = FinancialAccount.NormalizeName(request.Name);
        var exists = await _accounts.ExistsAsync(tenantId, normalizedName, excludeId: null, cancellationToken);
        if (exists)
        {
            return Result<Guid>.Failure(new ResultError(
                "Já existe uma conta com este nome.",
                nameof(request.Name)));
        }

        var account = new FinancialAccount(tenantId, request.Name, request.Type.Value, request.InitialBalance.Value);
        await _accounts.AddAsync(account, cancellationToken);

        return Result<Guid>.Success(account.Id);
    }
}
