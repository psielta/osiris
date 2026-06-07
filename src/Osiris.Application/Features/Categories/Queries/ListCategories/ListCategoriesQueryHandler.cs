using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.Categories.DTOs;

namespace Osiris.Application.Features.Categories.Queries.ListCategories;

public sealed class ListCategoriesQueryHandler
    : IRequestHandler<ListCategoriesQuery, IReadOnlyCollection<CategoryListItemDto>>
{
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;

    public ListCategoriesQueryHandler(ICategoryRepository categories, ICurrentUser currentUser)
    {
        _categories = categories;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<CategoryListItemDto>> Handle(
        ListCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _categories.ListAsync(
            _currentUser.TenantId,
            request.IncludeArchived,
            cancellationToken);

        return categories
            .Select(category => new CategoryListItemDto(
                category.Id,
                category.Name,
                category.Type,
                category.Color,
                category.IsActive))
            .ToArray();
    }
}
