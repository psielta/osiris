using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.CreditCardPurchases.Commands.DeleteCreditCardPurchase;

public sealed class DeleteCreditCardPurchaseCommandHandler : IRequestHandler<DeleteCreditCardPurchaseCommand, Result>
{
    private readonly ICreditCardPurchaseRepository _purchases;
    private readonly ICreditCardInstallmentRepository _installments;
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteCreditCardPurchaseCommandHandler(
        ICreditCardPurchaseRepository purchases,
        ICreditCardInstallmentRepository installments,
        ICreditCardStatementRepository statements,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _purchases = purchases;
        _installments = installments;
        _statements = statements;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteCreditCardPurchaseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var purchase = await _purchases.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (purchase is null)
        {
            return Result.Failure(new ResultError("Compra não encontrada.", Code: ResultErrorCodes.NotFound));
        }

        var installments = await _installments.ListByPurchaseAsync(tenantId, purchase.Id, cancellationToken);
        var statementIds = installments
            .Select(installment => installment.CreditCardStatementId)
            .Distinct()
            .ToArray();
        var statements = await _statements.ListByIdsAsync(tenantId, statementIds, cancellationToken);

        if (statements.Any(statement => statement.Status == CreditCardStatementStatus.Paid))
        {
            return Result.Failure(new ResultError("Não é possível excluir uma compra com fatura já paga."));
        }

        var totals = await _statements.GetTotalsAsync(tenantId, statementIds, cancellationToken);
        var utcNow = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);
        foreach (var statement in statements)
        {
            var removedAmount = installments
                .Where(installment => installment.CreditCardStatementId == statement.Id)
                .Sum(installment => installment.Amount);
            var statementTotals = totals[statement.Id];

            statement.RefreshStatus(
                statementTotals.InstallmentsTotal - removedAmount,
                statementTotals.PaymentsTotal,
                today,
                utcNow);
        }

        await _purchases.DeleteAsync(purchase, installments, cancellationToken);

        return Result.Success();
    }
}
