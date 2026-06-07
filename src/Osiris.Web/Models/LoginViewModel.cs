using System.ComponentModel.DataAnnotations;

namespace Osiris.Web.Models;

public sealed class LoginViewModel
{
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Lembrar de mim")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
