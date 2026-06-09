using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.CreditCardStatementPayments.Commands.RegisterCreditCardStatementPayment;

public sealed class RegisterCreditCardStatementPaymentCommandHandler
    : IRequestHandler<RegisterCreditCardStatementPaymentCommand, Result<Guid>>
{
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICreditCardStatementPaymentRepository _payments;
    private readonly ICreditCardRepository _creditCards;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterCreditCardStatementPaymentCommandHandler(
        ICreditCardStatementRepository statements,
        ICreditCardStatementPaymentRepository payments,
        ICreditCardRepository creditCards,
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _statements = statements;
        _payments = payments;
        _creditCards = creditCards;
        _accounts = accounts;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(
        RegisterCreditCardStatementPaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe o valor do pagamento.", nameof(request.Amount)));
        }

        if (request.PaidAt is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe a data do pagamento.", nameof(request.PaidAt)));
        }

        var tenantId = _currentUser.TenantId;
        var statement = await _statements.GetByIdAsync(tenantId, request.StatementId, cancellationToken);
        if (statement is null)
        {
            return Result<Guid>.Failure(new ResultError("Fatura não encontrada.", Code: ResultErrorCodes.NotFound));
        }

        var totalsById = await _statements.GetTotalsAsync(tenantId, new[] { statement.Id }, cancellationToken);
        var totals = totalsById[statement.Id];

        if (totals.OpenBalance <= 0m)
        {
            return Result<Guid>.Failure(new ResultError("A fatura não possui saldo em aberto."));
        }

        if (request.Amount.Value > totals.OpenBalance)
        {
            return Result<Guid>.Failure(new ResultError(
                "O pagamento não pode ser maior que o saldo em aberto.",
                nameof(request.Amount)));
        }

        var payment = new CreditCardStatementPayment(
            tenantId,
            statement.Id,
            request.FinancialAccountId,
            request.Amount.Value,
            request.PaidAt.Value,
            request.Notes);

        var utcNow = _dateTimeProvider.UtcNow;
        FinancialAccountMovement? movement = null;
        FinancialAccount? account = null;
        if (request.FinancialAccountId is not null)
        {
            account = await _accounts.GetByIdAsync(tenantId, request.FinancialAccountId.Value, cancellationToken);
            if (account is null)
            {
                return Result<Guid>.Failure(new ResultError(
                    "Conta não encontrada.",
                    nameof(request.FinancialAccountId)));
            }

            if (!account.IsActive)
            {
                return Result<Guid>.Failure(new ResultError(
                    "A conta está arquivada.",
                    nameof(request.FinancialAccountId)));
            }

            var card = await _creditCards.GetByIdAsync(tenantId, statement.CreditCardId, cancellationToken);
            var cardName = card?.Name ?? "cartão";

            // Paying a statement settles card debt: it is a cash outflow only, never a second
            // categorized expense — the expense was recorded when the purchase happened.
            movement = new FinancialAccountMovement(
                tenantId,
                account.Id,
                FinancialAccountMovementType.CreditCardStatementPayment,
                request.Amount.Value,
                request.PaidAt.Value,
                $"Pagamento de fatura {cardName} {statement.ReferenceMonth:00}/{statement.ReferenceYear}",
                categoryId: null,
                relatedEntityType: nameof(CreditCardStatementPayment),
                relatedEntityId: payment.Id);

            account.ApplyMovement(FinancialAccountMovementType.CreditCardStatementPayment, request.Amount.Value, utcNow);
        }

        statement.RefreshStatus(
            totals.InstallmentsTotal,
            totals.PaymentsTotal + request.Amount.Value,
            DateOnly.FromDateTime(utcNow),
            utcNow);

        await _payments.AddAsync(payment, statement, movement, account, cancellationToken);

        return Result<Guid>.Success(payment.Id);
    }
}
