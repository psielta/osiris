using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.CreditCardPurchases.Commands.ChangeCreditCardPurchaseCategory;

public sealed class ChangeCreditCardPurchaseCategoryCommandHandler
    : IRequestHandler<ChangeCreditCardPurchaseCategoryCommand, Result>
{
    private readonly ICreditCardPurchaseRepository _purchases;
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeCreditCardPurchaseCategoryCommandHandler(
        ICreditCardPurchaseRepository purchases,
        ICategoryRepository categories,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _purchases = purchases;
        _categories = categories;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ChangeCreditCardPurchaseCategoryCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var purchase = await _purchases.GetByIdAsync(tenantId, request.PurchaseId, cancellationToken);
        if (purchase is null)
        {
            return Result.Failure(new ResultError("Compra não encontrada.", Code: ResultErrorCodes.NotFound));
        }

        var category = await _categories.GetByIdAsync(tenantId, request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
        {
            return Result.Failure(new ResultError(
                "Categoria não encontrada ou arquivada.",
                nameof(request.CategoryId)));
        }

        if (category.Type != CategoryType.Expense)
        {
            return Result.Failure(new ResultError(
                "A categoria da compra deve ser uma categoria de despesa.",
                nameof(request.CategoryId)));
        }

        purchase.ChangeCategory(category.Id, _dateTimeProvider.UtcNow);
        await _purchases.UpdateAsync(purchase, cancellationToken);

        return Result.Success();
    }
}
