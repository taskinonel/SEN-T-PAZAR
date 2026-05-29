using System.ComponentModel.DataAnnotations;

namespace SEN_T_PAZAR.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta veya kullanıcı adı zorunludur.")]
    [Display(Name = "E-posta veya kullanıcı adı")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni Hatırla")]
    public bool RememberMe { get; set; }
}
