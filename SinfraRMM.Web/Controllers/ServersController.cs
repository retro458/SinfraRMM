using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinfraRMM.Web.Dtos;
using SinfraRMM.Web.Models;
using SinfraRMM.Web.Services;

namespace SinfraRMM.Web.Controllers
{
    public class ServersController : Controller
    {
        private readonly ApiClient _api;

        public ServersController(ApiClient api)
        {
            _api = api;
        }


        //GET /Server

        public async Task<IActionResult>Index()
        {
            var response = await _api.GetAsync<ApiResponse<List<ServerDto>>>("api/servers");
            ViewBag.ApiUrl = DotNetEnv.Env.GetString("API_URL");
            return View(response?.Data ?? new List<ServerDto>());
        }


        // GET /Servers/Detail/{id}
        public async Task<IActionResult>Detail(Guid id)
        {
            var server = await _api.GetAsync<ApiResponse<ServerDto>>($"api/servers/{id}");
            if (server?.Data is null) return NotFound();

            var history = await _api.GetAsync<ApiResponse<List<CommandHistoryDto>>>($"api/commands/history/{id}");

            var vm = new ServerDetailViewModel
            {
                Server = server.Data,
                History = history?.Data ?? new List<CommandHistoryDto>()
            };

            return View(vm);
        }

        // GET /Servers/Console/{id}
        public async Task<IActionResult> Console(Guid id)
        {
            var server = await _api.GetAsync<ApiResponse<ServerDto>>($"api/servers/{id}");
            if(server?.Data is null) return NotFound();

            var commands = await _api.GetAsync<ApiResponse<List<CommandDto>>>("api/commands/library");

            var vm = new ServerConsoleViewModel
            {
                Server = server.Data,
                Commands = commands?.Data ?? new List<CommandDto>()
            };

            ViewBag.ApiUrl = DotNetEnv.Env.GetString("API_URL");
            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles ="Admin, Tecnico")]
        public async Task<IActionResult>Execute(Guid serverId, int commandId)
        {
            await _api.PostAsync("api/commands/execute", new {serverId, commandId});
            return RedirectToAction("Console", new {id = serverId});
        }
    

    [HttpPost]
    [Authorize(Roles = "Admin,Tecnico")]
    public async Task<IActionResult> ExecuteCommand([FromBody] ExecuteCommandRequest request)
        {
            var res = await _api.PostAsync("api/commands/execute", new
            {
               serverId = request.ServerId,
               commandId = request.CommandId 
            });
               if (res.IsSuccessStatusCode)
                  return Ok();

            return StatusCode((int)res.StatusCode);
        }
        

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateServerRequest request)
        {
            var res = await _api.PostAsync("api/servers", new
            {
               name = request.Name,
               ipAddress = request.IpAddress,
               category = request.Category,
               osInfo = request.OsInfo 
            });

            if(res.IsSuccessStatusCode)
            return Ok();

            var body = await res.Content.ReadAsStringAsync();
            return StatusCode((int)res.StatusCode,
                System.Text.Json.JsonSerializer.Deserialize<object>(body));
        }

    }
    public class ExecuteCommandRequest
    {
    public Guid ServerId { get; set; }
    public int CommandId { get; set; }
    }
    public class CreateServerRequest
    {
        public string Name {get;set;} = string.Empty;
        public string IpAddress{get;set;} = string.Empty;
        public string Category{get;set;} = string.Empty;
        public string? OsInfo {get;set;}
    }
}

