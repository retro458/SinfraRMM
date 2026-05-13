using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SinfraRMM.API.Data;
using SinfraRMM.API.Dtos;
using SinfraRMM.API.Interfaces;
using SinfraRMM.API.Models;

namespace SinfraRMM.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
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
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Provider == request.Provider);

            if (user == null)
            {
                var roltecnico = await _context.Roles.FirstAsync(r => r.Name == "Técnico")
                    ?? throw new InvalidOperationException("Rol 'Técnico' no encontrado");
                user = new User
                {
                    Email = request.Email,
                    RoleId = roltecnico.Id,
                    Provider = request.Provider,
                    ExternalId = request.ExternalId,
                    AvatarUrl = request.AvatarUrl,
                    CreatedAt = DateTime.UtcNow,
                    password = null! // No se usa para logins externos, pero la propiedad es requerida por el modelo.
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                user.Role = roltecnico;
            }
            else if (user.ExternalId != request.ExternalId)
            {
                throw new UnauthorizedAccessException("credenciales inválidas");
            }

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
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("provider", user.Provider)
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
            Role = user.Role.Name,
            Provider = user.Provider,
            AvatarUrl = user.AvatarUrl
        };
    }
}