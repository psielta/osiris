using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCardPurchases.DTOs;

namespace Osiris.Application.Features.CreditCardPurchases.Queries.ListCreditCardPurchases;

public sealed class ListCreditCardPurchasesQueryHandler
    : IRequestHandler<ListCreditCardPurchasesQuery, IReadOnlyCollection<CreditCardPurchaseListItemDto>>
{
    private readonly ICreditCardPurchaseRepository _purchases;
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;

    public ListCreditCardPurchasesQueryHandler(
        ICreditCardPurchaseRepository purchases,
        ICategoryRepository categories,
        ICurrentUser currentUser)
    {
        _purchases = purchases;
        _categories = categories;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<CreditCardPurchaseListItemDto>> Handle(
        ListCreditCardPurchasesQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var purchases = await _purchases.ListByCardAsync(tenantId, request.CreditCardId, cancellationToken);
        var categories = await _categories.ListAsync(tenantId, includeArchived: true, cancellationToken);
        var categoryNames = categories.ToDictionary(category => category.Id, category => category.Name);

        return purchases
            .Select(purchase => new CreditCardPurchaseListItemDto(
                purchase.Id,
                purchase.Description,
                categoryNames.GetValueOrDefault(purchase.CategoryId),
                purchase.TotalAmount,
                purchase.PurchaseDate,
                purchase.Installments))
            .ToArray();
    }
}
