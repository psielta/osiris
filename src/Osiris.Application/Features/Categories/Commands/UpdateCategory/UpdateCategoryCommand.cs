using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    CategoryType? Type,
    string? Color) : IRequest<Result>;
