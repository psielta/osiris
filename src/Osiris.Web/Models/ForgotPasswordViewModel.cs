using System.ComponentModel.DataAnnotations;

namespace Osiris.Web.Models;

public sealed class ForgotPasswordViewModel
{
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;
}
