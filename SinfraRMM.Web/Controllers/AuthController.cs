using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SinfraRMM.Web.Models;
using SinfraRMM.Web.Services;
using SinfraRMM.Web.Models.Auth;
namespace SinfraRMM.Web.Controllers;


public class AuthController : Controller
{
    private readonly ApiClient _apiClient;

    public AuthController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // GET: /Auth/Login
    [HttpGet]
    //[ValidateAntiForgeryToken]
    public IActionResult Login(string? returnUrl = null)
    {
         if(User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new LoginViewModel { returnUrl = returnUrl });
    }

    // POST: /Auth/Login
    [HttpPost]
    //[ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
       if(!ModelState.IsValid)
            return View(model);

        var res = await _apiClient.PostAsync(" api/auth/login", new
        {
            email = model.Email,
            password = model.Password
        });
        if(!res.IsSuccessStatusCode)
        {
            model.error = "Credenciales inválidas";
            return View(model);
        }

        // leer body de la respuesta
        var body = await res.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<AuthDataDto>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if(response?.Data is null)
        {
            model.error = "Error al procesar la respuesta del servidor";
            return View(model);
        }

        // La cookie jwt ya la seteo la api en el reponse
        // propagar en el browser desde el mvc
        var apicookie = res.Headers
            .FirstOrDefault(h => h.Key == "Set-Cookie").Value?
            .FirstOrDefault(c => c.StartsWith("X-Access-Token="));

        if(apicookie is not null)
        {
            var tokenValue = apicookie.Split(';')[0].Replace("X-Access-Token=", "");
            Response.Cookies.Append("X-Access-Token", tokenValue, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });
        }
            // crear la sesion propia del mvc con los claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, response.Data.Email),
                new Claim(ClaimTypes.Role, response.Data.Role),
                new Claim("Provider", response.Data.Provider ?? "Local"),
            };

            if (response.Data.AvatarUrl is not null)
                claims.Add(new Claim("AvatarUrl", response.Data.AvatarUrl));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
                return RedirectToAction(model .returnUrl ?? "Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
       //CERRAMOS SESION EN LA API
         await _apiClient.PostAsync("api/auth/logout", new { });

         //CERARAR SESION EN EL MVC
         await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

         //ELIMINAR LA COOKIE JWT
         Response.Cookies.Delete("X-Access-Token");

            return RedirectToAction("Login");
    }

    //get: /Auth/AccessDenied
    public IActionResult AccessDenied() => View();


    // ===========================
    // esto es un OAUTH placeholder, no implementado aun
    // ===========================

    [HttpGet]
public IActionResult ExternalLogin(string provider)
{
    var redirectUrl = Url.Action("ExternalCallback", "Auth");
    var properties  = new AuthenticationProperties { RedirectUri = redirectUrl };
    return Challenge(properties, provider);
}

// GET /Auth/ExternalCallback
[HttpGet]
public async Task<IActionResult> ExternalCallback()
{
    // Lee el resultado del proveedor OAuth
    var result = await HttpContext.AuthenticateAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);

         Console.WriteLine($"[DEBUG] OAuth succeeded: {result.Succeeded}");
    Console.WriteLine($"[DEBUG] Provider: {result.Properties?.Items[".AuthScheme"]}");

    if (!result.Succeeded)
    {
        TempData["Error"] = "Error al autenticar con el proveedor externo.";
        return RedirectToAction("Login");
    }

    var claims   = result.Principal?.Claims;
    var email    = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    var name     = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
    var avatar   = claims?.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value
                ?? claims?.FirstOrDefault(c => c.Type == "picture")?.Value;

    // Detecta el proveedor
    var provider   = result.Properties?.Items[".AuthScheme"] ?? "Google";
    var externalId = claims?.FirstOrDefault(c =>
        c.Type == ClaimTypes.NameIdentifier)?.Value;

    if (email is null || externalId is null)
    {
        TempData["Error"] = "No se pudo obtener el email del proveedor.";
        return RedirectToAction("Login");
    }

    // Llama a la API para registrar o loguear
    var res = await _apiClient.PostAsync("api/auth/external", new
    {
        email      = email,
        provider   = provider,
        externalId = externalId,
        avatarUrl  = avatar
    });
     Console.WriteLine($"[DEBUG] API response: {res.StatusCode}");

    var body = await res.Content.ReadAsStringAsync();
     Console.WriteLine($"[DEBUG] API body: {body}");
    var json  = System.Text.Json.JsonDocument.Parse(body).RootElement;

    // Usuario pendiente de aprobación
    if (json.TryGetProperty("pending", out var pending) && pending.GetBoolean())
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Pending"] = "Tu cuenta está pendiente de aprobación por un administrador.";
        return RedirectToAction("Login");
    }

    if (!res.IsSuccessStatusCode)
    {
        TempData["Error"] = "Error al iniciar sesión.";
        return RedirectToAction("Login");
    }

    // Propaga la cookie JWT de la API
    var apiCookie = res.Headers
        .FirstOrDefault(h => h.Key == "Set-Cookie").Value?
        .FirstOrDefault(c => c.StartsWith("X-Access-Token"));

    if (apiCookie is not null)
    {
        var tokenValue = apiCookie.Split(';')[0].Replace("X-Access-Token=", "");
        Response.Cookies.Append("X-Access-Token", tokenValue, new CookieOptions
        {
            HttpOnly = true,
            Secure   = false, // true en producción
            SameSite = SameSiteMode.Lax,
            Expires  = DateTimeOffset.UtcNow.AddHours(8)
        });
    }

    // Crea sesión MVC con claims
    var roleValue = json.GetProperty("data").GetProperty("role").GetString() ?? "Tecnico";
    var mvcClaims = new List<Claim>
    {
        new(ClaimTypes.Email,  email),
        new(ClaimTypes.Role,   roleValue),
        new("provider",        provider),
        new("avatar",          avatar ?? "")
    };

    var identity  = new ClaimsIdentity(mvcClaims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(8)
        });

    return RedirectToAction("Index", "Dashboard");
}
    
}