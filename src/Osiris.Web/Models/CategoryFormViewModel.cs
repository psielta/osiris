using System.ComponentModel.DataAnnotations;
using Osiris.Domain.Enums;

namespace Osiris.Web.Models;

public sealed class CategoryFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Tipo")]
    public CategoryType? Type { get; set; }

    [Display(Name = "Cor")]
    public string? Color { get; set; }
}
