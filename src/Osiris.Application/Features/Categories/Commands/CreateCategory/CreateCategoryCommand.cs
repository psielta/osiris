using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    CategoryType? Type,
    string? Color) : IRequest<Result<Guid>>;
