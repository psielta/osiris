using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Application.Features.FinancialAccounts.DTOs;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;

public sealed class GetFinancialAccountDetailsQueryHandler
    : IRequestHandler<GetFinancialAccountDetailsQuery, FinancialAccountStatementDto?>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly IFinancialAccountMovementRepository _movements;
    private readonly ICurrentUser _currentUser;

    public GetFinancialAccountDetailsQueryHandler(
        IFinancialAccountRepository accounts,
        IFinancialAccountMovementRepository movements,
        ICurrentUser currentUser)
    {
        _accounts = accounts;
        _movements = movements;
        _currentUser = currentUser;
    }

    public async Task<FinancialAccountStatementDto?> Handle(
        GetFinancialAccountDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var account = await _accounts.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var movements = await _movements.ListByAccountAsync(tenantId, account.Id, cancellationToken);

        return new FinancialAccountStatementDto(
            account.Id,
            account.Name,
            account.Type,
            account.InitialBalance,
            account.CurrentBalance,
            account.IsActive,
            movements
                .Select(movement => new MovementListItemDto(
                    movement.Id,
                    movement.Type,
                    movement.Amount,
                    movement.Type.IsInflow(),
                    movement.OccurredOn,
                    movement.Description,
                    movement.CategoryId,
                    movement.Notes))
                .ToArray());
    }
}
