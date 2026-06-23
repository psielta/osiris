using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCardPurchases.DTOs;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.CreditCardPurchases.Queries.GetCreditCardPurchaseDetails;

public sealed class GetCreditCardPurchaseDetailsQueryHandler
    : IRequestHandler<GetCreditCardPurchaseDetailsQuery, CreditCardPurchaseDetailsDto?>
{
    private readonly ICreditCardPurchaseRepository _purchases;
    private readonly ICreditCardInstallmentRepository _installments;
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICreditCardRepository _creditCards;
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;

    public GetCreditCardPurchaseDetailsQueryHandler(
        ICreditCardPurchaseRepository purchases,
        ICreditCardInstallmentRepository installments,
        ICreditCardStatementRepository statements,
        ICreditCardRepository creditCards,
        ICategoryRepository categories,
        ICurrentUser currentUser)
    {
        _purchases = purchases;
        _installments = installments;
        _statements = statements;
        _creditCards = creditCards;
        _categories = categories;
        _currentUser = currentUser;
    }

    public async Task<CreditCardPurchaseDetailsDto?> Handle(
        GetCreditCardPurchaseDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var purchase = await _purchases.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (purchase is null)
        {
            return null;
        }

        var card = await _creditCards.GetByIdAsync(tenantId, purchase.CreditCardId, cancellationToken);
        var category = await _categories.GetByIdAsync(tenantId, purchase.CategoryId, cancellationToken);
        var installments = await _installments.ListByPurchaseAsync(tenantId, purchase.Id, cancellationToken);

        var statementIds = installments
            .Select(installment => installment.CreditCardStatementId)
            .Distinct()
            .ToArray();
        var statements = await _statements.ListByIdsAsync(tenantId, statementIds, cancellationToken);
        var statementsById = statements.ToDictionary(statement => statement.Id);

        var installmentItems = installments
            .OrderBy(installment => installment.InstallmentNumber)
            .Select(installment =>
            {
                var statement = statementsById.GetValueOrDefault(installment.CreditCardStatementId);
                return new CreditCardPurchaseInstallmentDto(
                    installment.Id,
                    installment.InstallmentNumber,
                    installment.TotalInstallments,
                    installment.Amount,
                    installment.DueDate,
                    installment.CreditCardStatementId,
                    statement?.ReferenceMonth ?? installment.DueDate.Month,
                    statement?.ReferenceYear ?? installment.DueDate.Year);
            })
            .ToArray();

        return new CreditCardPurchaseDetailsDto(
            purchase.Id,
            purchase.CreditCardId,
            card?.Name ?? string.Empty,
            category?.Name,
            purchase.CategoryId,
            purchase.Description,
            purchase.TotalAmount,
            purchase.PurchaseDate,
            purchase.Installments,
            purchase.Notes,
            installmentItems);
    }
}
