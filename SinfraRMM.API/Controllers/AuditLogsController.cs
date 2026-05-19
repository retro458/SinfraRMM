using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SinfraRMM.API.Data;
using SinfraRMM.API.Dtos;

namespace SinfraRMM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuditLogsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? ServerId,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _db.AuditLogs
                .Include(a => a.Command)
                .Include(a => a.Server)
                .Include(a => a.User)
                .AsQueryable();

            if (ServerId.HasValue)
                query = query.Where(a => a.ServerId == ServerId);
            if(!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status);

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize) 
                .Select(a => new
                {
                  id = a.Id,
                  server = a.Server!.Name,
                  assetCode = a.Server.AssetCode,
                  command = a.Command !=null ? a.Command.Name : "—",
                  actionOutput = a.ActionOutput,
                  status = a.Status,
                  user = a.User !=null ? a.User.Email : "Sistema",
                  createdAt = a.CreatedAt  
                })
                .ToListAsync();

            return Ok(RespuestaDto.Ok("Audit logs obtenidos." , new {total, page, pageSize, logs}));
            }  
      }

 } 
