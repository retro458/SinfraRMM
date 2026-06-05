using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinfraRMM.Web.Services;

namespace SinfraRMM.Web.Controllers
{
   [Authorize]
    public class ReportsController : Controller
    {
        private readonly HttpClient _httpClient;

        public ReportsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); 
        }

        [HttpGet]
        public async Task<IActionResult> DownloadGeneralReport()
        {
            // Extraer el token de la cookie del MVC para enviarlo a la API
            var token = Request.Cookies["X-Access-Token"];
            
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            // Hacemos la petición a la API
            var response = await _httpClient.GetAsync("/api/reports/general");

            if (response.IsSuccessStatusCode)
            {
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/pdf";
                var fileName = $"SinfraRMM-Reporte-{DateTime.Now:yyyyMMdd-HHmm}.pdf";
                
                // Devolvemos el archivo al usuario
                return File(fileBytes, contentType, fileName);
            }

            // Manejo de error si la API falla
            TempData["Error"] = "No se pudo generar el reporte.";
            return RedirectToAction("Index", "Home");
        }
    }
}