using Osiris.Domain.Enums;

namespace Osiris.Web.Models;

public sealed class CategoryFormViewModel
{
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public CategoryType? Type { get; set; }

    public string? Color { get; set; }
}
