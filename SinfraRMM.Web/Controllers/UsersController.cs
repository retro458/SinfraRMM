using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinfraRMM.Web.Dtos;
using SinfraRMM.Web.Models;
using SinfraRMM.Web.Services;

namespace SinfraRMM.Web.Controllers
{

    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApiClient _api;
        public UsersController(ApiClient api)
        {
            _api = api;
        }


        public async Task<IActionResult> Index()
        {
            var users = await _api.GetAsync<ApiResponse<List<UserDto>>>("api/user");
             foreach (var u in users?.Data ?? new List<UserDto>())
        Console.WriteLine($"[DEBUG] {u.Email} - Status: {u.Status}");
            return View(users?.Data ?? new List<UserDto>());
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole([FromBody] ChangeRoleRequest request)
        {
            var res = await _api.PatchAsync($"api/user/{request.UserId}/role", new {RoleId = request.RoleId});
            if(res.IsSuccessStatusCode) return Ok();

            var body = await res.Content.ReadAsStringAsync();
            if(string.IsNullOrWhiteSpace(body))
                return StatusCode((int)res.StatusCode);

            return StatusCode((int)res.StatusCode,
                System.Text.Json.JsonSerializer.Deserialize<object>(body));
                
        }


        [HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Approve([FromBody] UserActionRequest request)
{
    var res = await _api.PatchAsync($"api/user/{request.UserId}/approve", new { });
    if (res.IsSuccessStatusCode) return Ok();
    return StatusCode((int)res.StatusCode);
}


[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Disable([FromBody] UserActionRequest request)
{
    var res = await _api.PatchAsync($"api/users/{request.UserId}/disable", new { });
    if (res.IsSuccessStatusCode) return Ok();
    return StatusCode((int)res.StatusCode);
}

public class UserActionRequest
{
    public Guid UserId { get; set; }
}

        public class ChangeRoleRequest
        {
            public Guid UserId { get; set; }
            public int RoleId { get; set; }
        }

    }

}