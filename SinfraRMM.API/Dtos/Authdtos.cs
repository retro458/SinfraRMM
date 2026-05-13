namespace SinfraRMM.API.Dtos
{

    // =====================================
    // REQUESTS DTOs
    // =====================================

    public class LoginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class RegisterDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

    }

    public class ExternalLoginDto
    {
        public string Email { get; set; } = null!;
        public string Provider { get; set; } = null!;
        public string ExternalId { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }

    // =====================================
    // RESPONSES DTOs
    // =====================================

    public class AuthResponseDto
    {
        
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    public class RespuestaDto
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        public object? Data { get; set; }

        public static RespuestaDto Ok(string mensaje, object? data = null) =>
            new() { Exito = true, Mensaje = mensaje, Data = data };

        public static RespuestaDto Error(string mensaje) =>
            new() { Exito = false, Mensaje = mensaje };
    }

}