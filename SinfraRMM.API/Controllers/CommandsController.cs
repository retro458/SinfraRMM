using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SinfraRMM.API.Data;
using SinfraRMM.API.Dtos;
using SinfraRMM.API.Models;
using System.Security.Claims;

namespace SinfraRMM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Tecnico")]
    public class CommandsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CommandsController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/commands/library
        // Lista todos los comandos disponibles (para mostrar en la UI tipo terminal)
        [HttpGet("library")]
        public async Task<IActionResult> GetLibrary()
        {
            var commands = await _db.CommandsLibraries
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.RequiresAdmin,
                    c.Category
                })
                .ToListAsync();

            return Ok(RespuestaDto.Ok("Comandos disponibles.", commands));
        }

        // POST /api/commands/execute
        // El técnico encola un comando para un servidor
        [HttpPost("execute")]
        public async Task<IActionResult> Execute([FromBody] ExecuteCommandDto dto)
        {
            // Verifica que el servidor exista
            var server = await _db.Servers.FindAsync(dto.ServerId);
            if (server is null)
                return NotFound(RespuestaDto.Error("Servidor no encontrado."));

            // Verifica que el comando exista en la whitelist
            var command = await _db.CommandsLibraries.FindAsync(dto.CommandId);
            if (command is null)
                return NotFound(RespuestaDto.Error("Comando no encontrado en la whitelist."));

            // Si el comando requiere admin, verifica el rol
            if (command.RequiresAdmin && !User.IsInRole("Admin"))
                return Forbid();

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var queueItem = new CommandQueue
            {
                ServerId = dto.ServerId,
                CommandId = dto.CommandId,
                RequestedBy = userId,
                status = "Pending",
                created_at = DateTime.UtcNow
            };

            _db.CommandQueue.Add(queueItem);
            await _db.SaveChangesAsync();

            return Ok(RespuestaDto.Ok("Comando encolado.", new { queueId = queueItem.Id }));
        }

        // GET /api/commands/history/{serverId}
        // Historial de comandos ejecutados en un servidor
        [HttpGet("history/{serverId:guid}")]
        public async Task<IActionResult> GetHistory(Guid serverId)
        {
            var history = await _db.AuditLogs
                .Include(a => a.Command)
                .Where(a => a.ServerId == serverId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(50)
                .Select(a => new
                {
                    a.Id,
                    command = a.Command!.Name,
                    a.ActionOutput,
                    a.Status,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(RespuestaDto.Ok("Historial de comandos.", history));
        }

        //POS /api/commands/add
        [HttpPost("add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add([FromBody] CommandDto dto)
        {
            var command = new CommandsLibrary
            {
                Name = dto.name,
                Description = dto.description,
                ActualCommand = dto.actual_command,
                RequiresAdmin = dto.requires_admin
            };

            _db.CommandsLibraries.Add(command);
            await _db.SaveChangesAsync();            

            return Ok(RespuestaDto.Ok("Comando agregado.", new { id = command.Id }));
        }
    }
}