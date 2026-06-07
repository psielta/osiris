using MediatR;
using Osiris.Application.Features.Categories.DTOs;

namespace Osiris.Application.Features.Categories.Queries.GetCategoryForEdit;

public sealed record GetCategoryForEditQuery(Guid Id) : IRequest<CategoryEditDto?>;
