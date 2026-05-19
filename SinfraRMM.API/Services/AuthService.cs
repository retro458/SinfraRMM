using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SinfraRMM.API.Data;
using SinfraRMM.API.Dtos;
using SinfraRMM.API.Hubs;
using SinfraRMM.API.Interfaces;
using SinfraRMM.API.Models;

namespace SinfraRMM.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<MonitorHub> _hub;

        public AuthService(AppDbContext context, IHubContext<MonitorHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public async Task<(AuthResponseDto dto, string token)> LoginAsync(LoginDto request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Provider == "Local")
                ?? throw new UnauthorizedAccessException("credenciales inválidas");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.password))
                throw new UnauthorizedAccessException("credenciales inválidas");
            return (MapToDto(user), GenerateToken(user));
        }

        public async Task<(AuthResponseDto dto, string token)> RegisterAsync(RegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new InvalidOperationException("El correo ya está registrado");

            var roltecnico = await _context.Roles.FirstAsync(r => r.Name == "Tecnico")
                ?? throw new InvalidOperationException("Rol 'Técnico' no encontrado");
            var user = new User
            {
                Email = request.Email,
                password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = roltecnico.Id,
                Provider = "Local",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            user.Role = roltecnico;

            return (MapToDto(user), GenerateToken(user));
        }

        public async Task<(AuthResponseDto dto, string token)> ExternalLoginAsync(ExternalLoginDto request)
{
    var user = await _context.Users
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u =>
            u.Provider == request.Provider &&
            u.ExternalId == request.ExternalId);

    if (user == null)
    {
        var techRole = await _context.Roles.FirstAsync(r => r.Name == "Tecnico");

        user = new User
        {
            Id        = Guid.NewGuid(),
            RoleId     = techRole.Id,
            Email      = request.Email,
            ExternalId = request.ExternalId,
            Provider   = request.Provider,
            AvatarUrl  = request.AvatarUrl,
            password   = null!,
            Status     = "Pending",  // <- entra como pendiente
            CreatedAt  = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        // Crea notificación para el admin
        var notification = new Notification
        {
            Type      = "NewUser",
            Message   = $"Nuevo usuario pendiente de aprobación: {request.Email}",
            IsRead    = false,
            Data      = System.Text.Json.JsonSerializer.Serialize(new
            {
                userId   = user.Id.ToString(),
                email    = request.Email,
                provider = request.Provider
            }),
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        user.Role = techRole;

        // Notifica al admin via SignalR
        await _hub.Clients.All.SendAsync("NewUserPending", new
        {
            userId   = user.Id,
            email    = user.Email,
            provider = user.Provider,
            message  = $" Nuevo usuario pendiente: {user.Email}"
        });

        // Redirige a pantalla de espera, no genera token
        throw new UnauthorizedAccessException("PENDING");
    }

    // Valida status antes de generar token
    if (user.Status == "Pending")
        throw new UnauthorizedAccessException("PENDING");

    if (user.Status == "Disabled")
        throw new UnauthorizedAccessException("Tu cuenta ha sido deshabilitada.");

    return (MapToDto(user), GenerateToken(user));
}

        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Env.GetString("JWT_KEY")));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role!.Name),
                new Claim("provider", user.Provider!)
            };

            var token = new JwtSecurityToken(
                issuer: Env.GetString("JWT_ISSUER"),
                audience: Env.GetString("JWT_AUDIENCE"),
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

         private static AuthResponseDto MapToDto(User user) => new()
        {
            Email = user.Email,
            Role = user.Role!.Name,
            Provider = user.Provider!,
            AvatarUrl = user.AvatarUrl
        };
    }
}