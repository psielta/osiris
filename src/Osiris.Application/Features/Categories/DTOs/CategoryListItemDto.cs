using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Categories.DTOs;

public sealed record CategoryListItemDto(
    Guid Id,
    string Name,
    CategoryType Type,
    string? Color,
    bool IsActive);
