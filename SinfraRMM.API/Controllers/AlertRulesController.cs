using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SinfraRMM.API.Data;
using SinfraRMM.API.Dtos;
using SinfraRMM.API.Models;

namespace SinfraRMM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AlertRulesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AlertRulesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? serverId)
        {
            var query = _db.AlertRules
                .Include(a => a.Server)
                .AsQueryable();

            if (serverId.HasValue)
                query = query.Where(a => a.ServerId == serverId);

            var rules = await query
                .OrderBy(a => a.Server!.AssetCode)
                .Select(a => new
                {
                    id         = a.Id,
                    serverId   = a.ServerId,
                    serverName = a.Server!.Name,
                    assetCode  = a.Server.AssetCode,
                    metricName = a.MetricName,
                    threshold  = a.Threshold,
                    @operator  = a.Operator,
                    isActive   = a.IsActive,
                    createdAt  = a.CreatedAt
                })
                .ToListAsync();

            return Ok(RespuestaDto.Ok("Reglas obtenidas.", rules));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateAlertRuleDto dto)
        {
            var server = await _db.Servers.FindAsync(dto.ServerId);
            if (server is null)
                return NotFound(RespuestaDto.Error("Servidor no encontrado."));

            var rule = new AlertRule
            {
                ServerId   = dto.ServerId,
                MetricName = dto.MetricName,
                Threshold  = dto.Threshold,
                Operator   = dto.Operator,
                IsActive   = true,
                CreatedAt  = DateTime.UtcNow
            };

            _db.AlertRules.Add(rule);
            await _db.SaveChangesAsync();

            return Ok(RespuestaDto.Ok("Regla creada.", new { id = rule.Id }));
        }

        [HttpPatch("{id:int}/toggle")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Toggle(int id)
        {
            var rule = await _db.AlertRules.FindAsync(id);
            if (rule is null)
                return NotFound(RespuestaDto.Error("Regla no encontrada."));

            rule.IsActive = !rule.IsActive;
            await _db.SaveChangesAsync();

            return Ok(RespuestaDto.Ok($"Regla {(rule.IsActive ? "activada" : "desactivada")}."));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _db.AlertRules.FindAsync(id);
            if (rule is null)
                return NotFound(RespuestaDto.Error("Regla no encontrada."));

            _db.AlertRules.Remove(rule);
            await _db.SaveChangesAsync();

            return Ok(RespuestaDto.Ok("Regla eliminada."));
        }
    }

    public class CreateAlertRuleDto
    {
        public Guid ServerId { get; set; }
        public string MetricName { get; set; } = null!; // CPU, RAM, DISK
        public decimal Threshold { get; set; }
        public string Operator { get; set; } = null!;   // >, <, =
    }
}