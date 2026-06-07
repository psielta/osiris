using MediatR;
using Osiris.Application.Features.Categories.DTOs;

namespace Osiris.Application.Features.Categories.Queries.ListCategories;

public sealed record ListCategoriesQuery(bool IncludeArchived = true)
    : IRequest<IReadOnlyCollection<CategoryListItemDto>>;
