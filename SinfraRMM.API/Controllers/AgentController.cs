// Controllers/AgentController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SinfraRMM.API.Data;
using SinfraRMM.API.Dtos;
using SinfraRMM.API.Hubs;
using SinfraRMM.API.Models;

namespace SinfraRMM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<MonitorHub> _hub;

        public AgentController(AppDbContext db, IHubContext<MonitorHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // ─────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────

        private async Task<Server?> ResolveServerAsync()
        {
            if (!Request.Headers.TryGetValue("X-Api-Key", out var key))
                return null;

            return await _db.Servers
                .FirstOrDefaultAsync(s => s.ApiKey == key.ToString());
        }

        // ─────────────────────────────────────────
        // POST /api/agent/heartbeat
        // El agente llama esto cada 30s
        // ─────────────────────────────────────────

         [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatDto dto)
    {
        var server = await ResolveServerAsync();
        if (server is null)
            return Unauthorized(RespuestaDto.Error("ApiKey inválida."));

        var wasOffline = server.Status == "Offline";

        server.Status = "Online";
        server.LastHeartbeat = DateTime.UtcNow;
        server.IpAddress = dto.IpAddress;
        server.OsInfo = dto.OsInfo;

        await _db.SaveChangesAsync();

        // Si estaba offline y volvió, notifica
        if (wasOffline)
        {
            await _hub.Clients
                .Group($"server-{server.Id}")
                .SendAsync("ServerStatusChanged", new
                {
                    serverId = server.Id,
                    status = "Online",
                    timestamp = DateTime.UtcNow
                });
        }

        return Ok(RespuestaDto.Ok($"[{server.AssetCode}] Heartbeat recibido."));
    }

        // ─────────────────────────────────────────
        // POST /api/agent/metrics
        // El agente manda CPU, RAM, disco cada 60s
        // ─────────────────────────────────────────

       [HttpPost("metrics")]
    public async Task<IActionResult> PostMetrics([FromBody] MetricDto dto)
    {
        var server = await ResolveServerAsync();
        if (server is null)
            return Unauthorized(RespuestaDto.Error("ApiKey inválida."));

        var metric = new MetricsHistory
        {
            ServerId = server.Id,
            CpuUsage = dto.CpuUsage,
            RamUsage = dto.RamUsage,
            DiskUsage = dto.DiskUsage,
            CreatedAt = DateTime.UtcNow
        };

        _db.MetricsHistories.Add(metric);
        await _db.SaveChangesAsync();

        // Notifica en tiempo real a todos los que escuchan este servidor
        await _hub.Clients
            .Group($"server-{server.Id}")
            .SendAsync("ReceiveMetrics", new
            {
                serverId = server.Id,
                cpu = dto.CpuUsage,
                ram = dto.RamUsage,
                disk = dto.DiskUsage,
                timestamp = DateTime.UtcNow
            });

        // Verifica alertas y notifica si algo supera el umbral
        await CheckAndNotifyAlertsAsync(server, dto);

        return Ok(RespuestaDto.Ok("Métricas registradas."));
    }
        // ─────────────────────────────────────────
        // GET /api/agent/pending-commands
        // El agente hace polling cada 5s
        // ─────────────────────────────────────────

        [HttpGet("pending-commands")]
        public async Task<IActionResult> GetPendingCommands()
        {
            var server = await ResolveServerAsync();
            if (server is null)
                return Unauthorized(RespuestaDto.Error("ApiKey inválida."));

            // Trae los pendientes y los marca como Executing
            var pending = await _db.CommandQueue
                .Include(q => q.Command)
                .Where(q => q.ServerId == server.Id && q.status == "Pending")
                .ToListAsync();

            foreach (var item in pending)
                item.status = "Executing";

            await _db.SaveChangesAsync();

            var result = pending.Select(q => new
            {
                queueId = q.Id,
                commandId = q.CommandId,
                actualCommand = q.Command.ActualCommand,
                name = q.Command.Name
            });

            return Ok(RespuestaDto.Ok("Comandos pendientes.", result));
        }

        // ─────────────────────────────────────────
        // POST /api/agent/command-result
        // El agente devuelve el output del comando
        // ─────────────────────────────────────────

         [HttpPost("command-result")]
    public async Task<IActionResult> CommandResult([FromBody] CommandResultDto dto)
    {
        var server = await ResolveServerAsync();
        if (server is null)
            return Unauthorized(RespuestaDto.Error("ApiKey inválida."));

        var queueItem = await _db.CommandQueue
            .Include(q => q.Command)
            .FirstOrDefaultAsync(q =>
                q.Id == dto.QueueId &&
                q.ServerId == server.Id &&
                q.status == "Executing");

        if (queueItem is null)
            return BadRequest(RespuestaDto.Error("Comando no encontrado en cola o ya procesado."));

        queueItem.status = dto.Status;
        queueItem.executed_at = DateTime.UtcNow;

        var log = new AuditLog
        {
            ServerId = server.Id,
            CommandId = queueItem.CommandId,
            UserId = queueItem.RequestedBy,
            ActionOutput = dto.Output,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();

        // Notifica al MVC que el comando terminó con su output
        await _hub.Clients
            .Group($"server-{server.Id}")
            .SendAsync("ReceiveCommandResult", new
            {
                queueId = dto.QueueId,
                commandName = queueItem.Command.Name,
                output = dto.Output,
                status = dto.Status,
                executedAt = queueItem.executed_at
            });

        return Ok(RespuestaDto.Ok("Resultado registrado."));
     }

      // Verifica las alert_rules y dispara notificación si se supera el umbral
    private async Task CheckAndNotifyAlertsAsync(Server server, MetricDto dto)
    {
        var rules = await _db.AlertRules
            .Where(r => r.ServerId == server.Id && r.IsActive)
            .ToListAsync();

        foreach (var rule in rules)
        {
            var currentValue = rule.MetricName switch
            {
                "CPU"  => dto.CpuUsage,
                "RAM"  => dto.RamUsage,
                "DISK" => dto.DiskUsage,
                _      => 0m
            };

            var triggered = rule.Operator switch
            {
                ">"  => currentValue > rule.Threshold,
                "<"  => currentValue < rule.Threshold,
                "="  => currentValue == rule.Threshold,
                _    => false
            };

            if (triggered)
            {
                await _hub.Clients
                    .Group($"server-{server.Id}")
                    .SendAsync("ReceiveAlert", new
                    {
                        serverId = server.Id,
                        serverName = server.Name,
                        metric = rule.MetricName,
                        currentValue,
                        threshold = rule.Threshold,
                        message = $"{server.Name}: {rule.MetricName} al {currentValue}% (umbral: {rule.Threshold}%)",
                        timestamp = DateTime.UtcNow
                    });
            }
        }
     }
  }
}