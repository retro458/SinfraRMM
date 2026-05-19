using DotNetEnv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinfraRMM.Web.Dtos;
using SinfraRMM.Web.Models;
using SinfraRMM.Web.Services;

namespace SinfraRMM.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApiClient _api;

        public DashboardController(ApiClient api)
        {
            _api = api;
        }


        public async Task<IActionResult> Index()
        {
            var response = await _api.GetAsync<ApiResponse<List<ServerDto>>>("api/servers");
            var servers = response?.Data ?? new List<ServerDto>();
            ViewBag.ApiUrl = Env.GetString("API_URL");
            return View(servers);
        }
    }




}