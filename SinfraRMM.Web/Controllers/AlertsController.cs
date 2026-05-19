using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinfraRMM.Web.Dtos;
using SinfraRMM.Web.Models;
using SinfraRMM.Web.Services;

namespace SinfraRMM.Web.Controllers
{
    [Authorize]
    public class AlertsController : Controller
    {
        private readonly ApiClient _api;

        public AlertsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var rules   = await _api.GetAsync<ApiResponse<List<AlertRuleDto>>>("api/alertrules");
            var servers = await _api.GetAsync<ApiResponse<List<ServerDto>>>("api/servers");

            ViewBag.Servers = servers?.Data ?? new List<ServerDto>();
            ViewBag.ApiUrl  = DotNetEnv.Env.GetString("API_URL");

            return View(rules?.Data ?? new List<AlertRuleDto>());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateAlertRequest request)
        {
            var res = await _api.PostAsync("api/alertrules", new
            {
                serverId   = request.ServerId,
                metricName = request.MetricName,
                threshold  = request.Threshold,
                @operator  = request.Operator
            });

            if (res.IsSuccessStatusCode) return Ok();

            var body = await res.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return StatusCode((int)res.StatusCode);

            return StatusCode((int)res.StatusCode,
                System.Text.Json.JsonSerializer.Deserialize<object>(body));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Toggle([FromBody] ToggleAlertRequest request)
        {
            var res = await _api.PatchAsync($"api/alertrules/{request.Id}/toggle", new { });
            if (res.IsSuccessStatusCode) return Ok();
            return StatusCode((int)res.StatusCode);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete([FromBody] DeleteAlertRequest request)
        {
            var res = await _api.DeleteAsync($"api/alertrules/{request.Id}");
            if (res.IsSuccessStatusCode) return Ok();
            return StatusCode((int)res.StatusCode);
        }
    }

    public class CreateAlertRequest
    {
        public Guid ServerId { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public decimal Threshold { get; set; }
        public string Operator { get; set; } = string.Empty;
    }

    public class ToggleAlertRequest { public int Id { get; set; } }
    public class DeleteAlertRequest { public int Id { get; set; } }
}