using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Categories.DTOs;

public sealed record CategoryEditDto(
    Guid Id,
    string Name,
    CategoryType Type,
    string? Color);
