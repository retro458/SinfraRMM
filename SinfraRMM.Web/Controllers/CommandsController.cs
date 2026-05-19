using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinfraRMM.Web.Dtos;
using SinfraRMM.Web.Models;
using SinfraRMM.Web.Services;

namespace SinfraRMM.Web.Controllers
{
        public class CommandsController : Controller
    {
        private readonly ApiClient _api;

        public CommandsController(ApiClient api)
        {
            _api = api;
        }

        // ==================
        // GET /Commands
        // ==================
        public async Task<IActionResult> Index()
        {
            var commands = await _api.GetAsync<ApiResponse<List<CommandDto>>>("api/commands/library");
            var commandsOrdered = commands?.Data?.OrderBy(c => c.Id).ToList();
            ViewBag.ApiUrl = DotNetEnv.Env.GetString("API_URL");
            return View(commandsOrdered ?? new List<CommandDto>());
        }


        // ==================
        // POST /Commands
        // ==================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add([FromBody] CreateCommandDto dto)
        {
            var res = await _api.PostAsync("api/commands/add", new
            {
                name = dto.Name,
                description = dto.Description,
                actual_command = dto.ActualCommand,
                requires_admin = dto.RequieresAdmin
            });

            if (res.IsSuccessStatusCode) return Ok();

            var body = await res.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
        return StatusCode((int)res.StatusCode);

    try
    {
        return StatusCode((int)res.StatusCode,
            System.Text.Json.JsonSerializer.Deserialize<object>(body));
    }
    catch
    {
        return StatusCode((int)res.StatusCode);
    }
        }   

     }  
}