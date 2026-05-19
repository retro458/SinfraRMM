using System.ComponentModel.DataAnnotations;

namespace SinfraRMM.Web.Models.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingrese un correo válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? returnUrl { get; set; }
    public string? error{ get; set; } 
}