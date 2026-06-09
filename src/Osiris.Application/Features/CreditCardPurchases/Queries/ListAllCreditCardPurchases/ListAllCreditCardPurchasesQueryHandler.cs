using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCardPurchases.DTOs;

namespace Osiris.Application.Features.CreditCardPurchases.Queries.ListAllCreditCardPurchases;

public sealed class ListAllCreditCardPurchasesQueryHandler
    : IRequestHandler<ListAllCreditCardPurchasesQuery, IReadOnlyCollection<CreditCardPurchaseOverviewDto>>
{
    private readonly ICreditCardPurchaseRepository _purchases;
    private readonly ICreditCardRepository _creditCards;
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;

    public ListAllCreditCardPurchasesQueryHandler(
        ICreditCardPurchaseRepository purchases,
        ICreditCardRepository creditCards,
        ICategoryRepository categories,
        ICurrentUser currentUser)
    {
        _purchases = purchases;
        _creditCards = creditCards;
        _categories = categories;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<CreditCardPurchaseOverviewDto>> Handle(
        ListAllCreditCardPurchasesQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var purchases = await _purchases.ListAsync(tenantId, cancellationToken);
        var cards = await _creditCards.ListAsync(tenantId, includeArchived: true, cancellationToken);
        var categories = await _categories.ListAsync(tenantId, includeArchived: true, cancellationToken);

        var cardNamesById = cards.ToDictionary(card => card.Id, card => card.Name);
        var categoryNamesById = categories.ToDictionary(category => category.Id, category => category.Name);

        return purchases
            .Select(purchase => new CreditCardPurchaseOverviewDto(
                purchase.Id,
                purchase.CreditCardId,
                cardNamesById.GetValueOrDefault(purchase.CreditCardId, "Cartão"),
                purchase.Description,
                categoryNamesById.GetValueOrDefault(purchase.CategoryId),
                purchase.TotalAmount,
                purchase.PurchaseDate,
                purchase.Installments))
            .ToArray();
    }
}
