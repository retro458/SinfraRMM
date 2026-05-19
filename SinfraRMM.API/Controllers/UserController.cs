using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SinfraRMM.API.Data;
using SinfraRMM.API.Dtos;

namespace SinfraRMM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UserController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
           var users = await _db.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Role!.Name)
                .Select(u => new
                {
                    id       = u.Id,
                    email    = u.Email,
                    role     = u.Role!.Name,
                    roleId   = u.RoleId,
                    provider = u.Provider,
                    avatarUrl = u.AvatarUrl,
                    status = u.Status,
                    createdAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(RespuestaDto.Ok("Usuarios", users));
        
        }


        [HttpPatch("{id:guid}/role")]
        public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null)
                return NotFound(RespuestaDto.Error("Usuario no encontrado."));

            var role = await _db.Roles.FindAsync(dto.RoleId);
            if (role is null)
                return BadRequest(RespuestaDto.Error("Rol no encontrado."));

            user.RoleId = dto.RoleId;
            await _db.SaveChangesAsync();

            return Ok(RespuestaDto.Ok("Rol cambiado a {role.Name}."));
        }

        [HttpPatch("{id:guid}/approve")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Approve(Guid id)
{
    var user = await _db.Users
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u => u.Id == id);

    if (user is null)
        return NotFound(RespuestaDto.Error("Usuario no encontrado."));

    user.Status = "Active";

    // Marca notificación como leída
    var idStr = id.ToString();
var notification = await _db.Notifications
    .Where(n => n.Type == "NewUser" && n.IsRead == false && n.Data != null)
    .ToListAsync(); // trae todas a memoria

// Filtra en memoria para evitar el problema de jsonb
var match = notification.FirstOrDefault(n => n.Data!.Contains(idStr));

if (match is not null)
    match.IsRead = true;
    await _db.SaveChangesAsync();

    return Ok(RespuestaDto.Ok($"Usuario {user.Email} aprobado."));
}

[HttpPatch("{id:guid}/disable")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Disable(Guid id)
{
    var user = await _db.Users.FindAsync(id);
    if (user is null)
        return NotFound(RespuestaDto.Error("Usuario no encontrado."));

    user.Status = "Disabled";
    await _db.SaveChangesAsync();

    return Ok(RespuestaDto.Ok($"Usuario deshabilitado."));
}
    }
}
        