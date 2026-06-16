using Osiris.Domain.Enums;

namespace Osiris.Api.Contracts;

public sealed record CreateCategoryRequest(string Name, CategoryType? Type, string? Color);

public sealed record UpdateCategoryRequest(string Name, CategoryType? Type, string? Color);
