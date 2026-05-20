using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using SinfraRMM.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// 1. Cargar .env
Env.Load();

// 2. Controladores con vistas
builder.Services.AddControllersWithViews();

// 3. HttpClient para consumir la API
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(Env.GetString("API_URL"));
});

// 4. Autenticación por cookies (sesión del MVC)
// El MVC no valida JWT directamente — solo guarda claims en cookie propia
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath        = "/Auth/Login";
    options.LogoutPath       = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan   = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly  = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddGoogle(options =>
{
    options.ClientId     = Env.GetString("GOOGLE_CLIENT_ID");
    options.ClientSecret = Env.GetString("GOOGLE_CLIENT_SECRET");
    options.CallbackPath = "/signin-google";
    options.Scope.Add("email");
    options.Scope.Add("profile");
});
builder.Services.AddAuthorization();

var app = builder.Build();

// 5. Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication(); // <- antes de Authorization
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "servers",
    pattern:"Servers/{action}/{id:guid}",
    defaults: new {controller = "Servers"});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}")

    .WithStaticAssets();

app.Run();
