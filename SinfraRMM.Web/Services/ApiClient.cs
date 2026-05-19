using System.Net.Http.Json;
using System.Text.Json;

namespace SinfraRMM.Web.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _ctx;
    
    public static readonly JsonSerializerOptions _json = new() 
    { 
        PropertyNameCaseInsensitive = true 
        
    };


    public ApiClient(HttpClient httpClient, IHttpContextAccessor ctx)
    {
        _httpClient = httpClient;
        _ctx = ctx;
    }

    // adjuntar la cookie jwt de la sesión  la request a la API
    private void AttachApiCookie()
    {
        _httpClient.DefaultRequestHeaders.Remove("Cookie");
        var token = _ctx.HttpContext?.Request.Cookies["X-Access-Token"];


        if(token is not null)
            _httpClient.DefaultRequestHeaders.Add("Cookie", $"X-Access-Token={token}");
    }


    public async Task<T> GetAsync<T>(string endpoint)
    {
        AttachApiCookie();
        var res = await _httpClient.GetAsync(endpoint);
        if(!res.IsSuccessStatusCode) return default!;
        return await res.Content.ReadFromJsonAsync<T>(_json) ?? default!;

    }

    public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T body)
    {
        AttachApiCookie();
        return await _httpClient.PostAsJsonAsync(endpoint, body);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T body)
    {
        AttachApiCookie();
        return await _httpClient.PutAsJsonAsync(endpoint, body);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
    {
        AttachApiCookie();
        return await _httpClient.DeleteAsync(endpoint);
    }

    // esto es para los endpoints del agente que usan ApiKey en lugar de cookie
    public async Task<HttpResponseMessage> PostWithApiKeyAsync<T>(string endpoint, T body, string apiKey)
    {
        _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        return await _httpClient.PostAsJsonAsync(endpoint, body);
    }


    public async Task<HttpResponseMessage> PatchAsync<T>(string endpoint, T body)
    {
        AttachApiCookie();
        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await _httpClient.PatchAsync(endpoint, content);
    }
}
