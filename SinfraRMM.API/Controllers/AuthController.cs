// Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinfraRMM.API.Dtos;
using SinfraRMM.API.Interfaces;

namespace SinfraRMM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _env;

        public AuthController(IAuthService authService, IWebHostEnvironment env)
        {
            _authService = authService;
            _env = env;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var (result, token) = await _authService.RegisterAsync(dto);
                SetAuthCookie(token);
                return Ok(RespuestaDto.Ok("Registro exitoso.", result));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(RespuestaDto.Error(ex.Message));
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var (result, token) = await _authService.LoginAsync(dto);
                SetAuthCookie(token);
                return Ok(RespuestaDto.Ok("Login exitoso.", result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(RespuestaDto.Error(ex.Message));
            }
        }

        // Lo llama  MVC después del callback de Google/Microsoft
     [HttpPost("external")]
public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginDto dto)
{
    try
    {
        var (result, token) = await _authService.ExternalLoginAsync(dto);
        SetAuthCookie(token);
        return Ok(RespuestaDto.Ok("Login externo exitoso.", result));
    }
    catch (UnauthorizedAccessException ex) when (ex.Message == "PENDING")
    {
        return Ok(new { pending = true, mensaje = "Tu cuenta está pendiente de aprobación." });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(RespuestaDto.Error(ex.Message));
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        // Log del inner exception real
        Console.WriteLine($"[DB ERROR] {ex.Message}");
        Console.WriteLine($"[DB INNER] {ex.InnerException?.Message}");
        return BadRequest(RespuestaDto.Error(ex.InnerException?.Message ?? ex.Message));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {ex.Message}");
        return BadRequest(RespuestaDto.Error(ex.Message));
    }
}

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("X-Access-Token");
            return Ok(RespuestaDto.Ok("Sesión cerrada."));
        }

        // Solo en desarrollo para poder ver el token en Swagger
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var provider = User.FindFirst("provider")?.Value;

            return Ok(RespuestaDto.Ok("Usuario autenticado.", new { email, role, provider }));
        }

        private void SetAuthCookie(string token)
        {
            Response.Cookies.Append("X-Access-Token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });
        }
    }
}