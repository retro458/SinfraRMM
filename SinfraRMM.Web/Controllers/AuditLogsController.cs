using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinfraRMM.Web.Dtos;
using SinfraRMM.Web.Models;
using SinfraRMM.Web.Services;

namespace SinfraRMM.Web.Controllers
{
    [Authorize(Roles = "Admin,Auditor")]
    public class AuditLogsController : Controller
    {
        private readonly ApiClient _api;

        public AuditLogsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(
            Guid? serverId = null,
            string? status = null,
            int page = 1)
        {
            var query = $"api/auditlogs?page={page}&pageSize=50";
            if (serverId.HasValue) query += $"&serverId={serverId}";
            if (!string.IsNullOrWhiteSpace(status)) query += $"&status={status}";

            var response = await _api.GetAsync<ApiResponse<AuditLogPagedDto>>(query);
            var servers  = await _api.GetAsync<ApiResponse<List<ServerDto>>>("api/servers");

            ViewBag.Servers    = servers?.Data ?? new List<ServerDto>();
            ViewBag.ServerId   = serverId;
            ViewBag.Status     = status;
            ViewBag.Page       = page;
            ViewBag.Total      = response?.Data?.Total ?? 0;
            ViewBag.PageSize   = 50;

            return View(response?.Data?.Logs ?? new List<AuditLogDto>());
        }
    }
}