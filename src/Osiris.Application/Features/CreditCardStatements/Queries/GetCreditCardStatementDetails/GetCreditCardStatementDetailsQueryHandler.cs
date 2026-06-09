using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.CreditCardStatements.Queries.GetCreditCardStatementDetails;

public sealed class GetCreditCardStatementDetailsQueryHandler
    : IRequestHandler<GetCreditCardStatementDetailsQuery, CreditCardStatementDetailsDto?>
{
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICreditCardInstallmentRepository _installments;
    private readonly ICreditCardPurchaseRepository _purchases;
    private readonly ICreditCardStatementPaymentRepository _payments;
    private readonly ICreditCardRepository _creditCards;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetCreditCardStatementDetailsQueryHandler(
        ICreditCardStatementRepository statements,
        ICreditCardInstallmentRepository installments,
        ICreditCardPurchaseRepository purchases,
        ICreditCardStatementPaymentRepository payments,
        ICreditCardRepository creditCards,
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _statements = statements;
        _installments = installments;
        _purchases = purchases;
        _payments = payments;
        _creditCards = creditCards;
        _accounts = accounts;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CreditCardStatementDetailsDto?> Handle(
        GetCreditCardStatementDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var statement = await _statements.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (statement is null)
        {
            return null;
        }

        var card = await _creditCards.GetByIdAsync(tenantId, statement.CreditCardId, cancellationToken);
        var installments = await _installments.ListByStatementAsync(tenantId, statement.Id, cancellationToken);
        var payments = await _payments.ListByStatementAsync(tenantId, statement.Id, cancellationToken);

        var purchaseIds = installments
            .Select(installment => installment.CreditCardPurchaseId)
            .Distinct()
            .ToArray();
        var purchases = await _purchases.ListByIdsAsync(tenantId, purchaseIds, cancellationToken);
        var purchaseDescriptions = purchases.ToDictionary(purchase => purchase.Id, purchase => purchase.Description);

        var accountNames = new Dictionary<Guid, string>();
        foreach (var accountId in payments
            .Where(payment => payment.FinancialAccountId is not null)
            .Select(payment => payment.FinancialAccountId!.Value)
            .Distinct())
        {
            var account = await _accounts.GetByIdAsync(tenantId, accountId, cancellationToken);
            if (account is not null)
            {
                accountNames[accountId] = account.Name;
            }
        }

        var totalAmount = installments.Sum(installment => installment.Amount);
        var paidAmount = payments.Sum(payment => payment.Amount);
        var status = CreditCardStatement.CalculateStatus(
            totalAmount,
            paidAmount,
            statement.ClosingDate,
            statement.DueDate,
            DateOnly.FromDateTime(_dateTimeProvider.UtcNow));

        return new CreditCardStatementDetailsDto(
            statement.Id,
            statement.CreditCardId,
            card?.Name ?? string.Empty,
            statement.ReferenceMonth,
            statement.ReferenceYear,
            statement.ClosingDate,
            statement.DueDate,
            status,
            totalAmount,
            paidAmount,
            totalAmount - paidAmount,
            installments
                .Select(installment => new CreditCardStatementInstallmentItemDto(
                    installment.Id,
                    installment.CreditCardPurchaseId,
                    purchaseDescriptions.GetValueOrDefault(installment.CreditCardPurchaseId, string.Empty),
                    installment.InstallmentNumber,
                    installment.TotalInstallments,
                    installment.Amount))
                .ToArray(),
            payments
                .OrderByDescending(payment => payment.PaidAt)
                .ThenByDescending(payment => payment.CreatedAtUtc)
                .Select(payment => new CreditCardStatementPaymentItemDto(
                    payment.Id,
                    payment.Amount,
                    payment.PaidAt,
                    payment.FinancialAccountId,
                    payment.FinancialAccountId is not null
                        ? accountNames.GetValueOrDefault(payment.FinancialAccountId.Value)
                        : null,
                    payment.Notes))
                .ToArray());
    }
}
