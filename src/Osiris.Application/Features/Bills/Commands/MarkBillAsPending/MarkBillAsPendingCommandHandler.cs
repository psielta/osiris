using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.Bills.Commands.MarkBillAsPending;

public sealed class MarkBillAsPendingCommandHandler : IRequestHandler<MarkBillAsPendingCommand, Result>
{
    private readonly IBillRepository _bills;
    private readonly IFinancialAccountMovementRepository _movements;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkBillAsPendingCommandHandler(
        IBillRepository bills,
        IFinancialAccountMovementRepository movements,
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _bills = bills;
        _movements = movements;
        _accounts = accounts;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(MarkBillAsPendingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var bill = await _bills.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (bill is null)
        {
            return Result.Failure(new ResultError("Conta não encontrada.", Code: ResultErrorCodes.NotFound));
        }

        if (!bill.IsPaid)
        {
            return Result.Failure(new ResultError("A conta já está pendente."));
        }

        var utcNow = _dateTimeProvider.UtcNow;

        // Reopening undoes the cash side of the payment: the movement is removed (not
        // compensated with a second row) and the account balance is restored.
        var movement = await _movements.GetByRelatedEntityAsync(tenantId, nameof(Bill), bill.Id, cancellationToken);
        FinancialAccount? account = null;
        if (movement is not null)
        {
            account = await _accounts.GetByIdAsync(tenantId, movement.FinancialAccountId, cancellationToken);
            account?.RevertMovement(movement.Type, movement.Amount, utcNow);
        }

        bill.MarkAsPending(utcNow);

        await _bills.SaveStatusChangeAsync(bill, movementToAdd: null, movement, account, cancellationToken);

        return Result.Success();
    }
}
