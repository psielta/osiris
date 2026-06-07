using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.Categories.DTOs;

namespace Osiris.Application.Features.Categories.Queries.GetCategoryForEdit;

public sealed class GetCategoryForEditQueryHandler : IRequestHandler<GetCategoryForEditQuery, CategoryEditDto?>
{
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;

    public GetCategoryForEditQueryHandler(ICategoryRepository categories, ICurrentUser currentUser)
    {
        _categories = categories;
        _currentUser = currentUser;
    }

    public async Task<CategoryEditDto?> Handle(GetCategoryForEditQuery request, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(_currentUser.TenantId, request.Id, cancellationToken);
        if (category is null)
        {
            return null;
        }

        return new CategoryEditDto(category.Id, category.Name, category.Type, category.Color);
    }
}
