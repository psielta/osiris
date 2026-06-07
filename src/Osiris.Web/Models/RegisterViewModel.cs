using System.ComponentModel.DataAnnotations;

namespace Osiris.Web.Models;

public sealed class RegisterViewModel
{
    [Display(Name = "Nome da área de trabalho")]
    public string TenantName { get; set; } = string.Empty;

    [Display(Name = "Nome completo")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Confirmar senha")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
