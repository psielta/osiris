using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Bills.Commands.MarkBillAsPaid;

public sealed class MarkBillAsPaidCommandHandler : IRequestHandler<MarkBillAsPaidCommand, Result>
{
    private readonly IBillRepository _bills;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkBillAsPaidCommandHandler(
        IBillRepository bills,
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _bills = bills;
        _accounts = accounts;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(MarkBillAsPaidCommand request, CancellationToken cancellationToken)
    {
        if (request.PaidAt is null)
        {
            return Result.Failure(new ResultError("Informe a data do pagamento.", nameof(request.PaidAt)));
        }

        var tenantId = _currentUser.TenantId;

        var bill = await _bills.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (bill is null)
        {
            return Result.Failure(new ResultError("Conta não encontrada.", Code: ResultErrorCodes.NotFound));
        }

        if (bill.IsPaid)
        {
            return Result.Failure(new ResultError("A conta já está marcada como paga."));
        }

        var utcNow = _dateTimeProvider.UtcNow;
        FinancialAccountMovement? movement = null;
        FinancialAccount? account = null;

        if (request.PaymentAccountId is not null)
        {
            account = await _accounts.GetByIdAsync(tenantId, request.PaymentAccountId.Value, cancellationToken);
            if (account is null)
            {
                return Result.Failure(new ResultError("Conta não encontrada.", nameof(request.PaymentAccountId)));
            }

            if (!account.IsActive)
            {
                return Result.Failure(new ResultError("A conta está arquivada.", nameof(request.PaymentAccountId)));
            }

            // Paying a bill is a cash outflow tied to the bill itself; the expense by category is
            // read from the bill, so the movement stays uncategorized to avoid double counting.
            movement = new FinancialAccountMovement(
                tenantId,
                account.Id,
                FinancialAccountMovementType.BillPayment,
                bill.Amount,
                request.PaidAt.Value,
                $"Pagamento de conta: {bill.Description}",
                categoryId: null,
                relatedEntityType: nameof(Bill),
                relatedEntityId: bill.Id);

            account.ApplyMovement(FinancialAccountMovementType.BillPayment, bill.Amount, utcNow);
        }

        bill.MarkAsPaid(request.PaidAt.Value, request.PaymentAccountId, utcNow);

        await _bills.SaveStatusChangeAsync(bill, movement, movementToRemove: null, account, cancellationToken);

        return Result.Success();
    }
}
