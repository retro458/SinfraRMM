using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SinfraRMM.API.Data;
using Microsoft.OpenApi.Models;
using SinfraRMM.API.Interfaces;
using SinfraRMM.API.Services;
using SinfraRMM.API.Models;
using SinfraRMM.API.Hubs;


var builder = WebApplication.CreateBuilder(args);

// 1. Cargar variables de entorno del archivo .env
Env.Load();

// 2. Configuración de la Base de Datos (PostgreSQL via Tailscale)
var connectionString = $"Host={Env.GetString("DB_HOST")};" +
                       $"Port={Env.GetString("DB_PORT")};" +
                       $"Database={Env.GetString("DB_NAME")};" +
                       $"Username={Env.GetString("DB_USER")};" +
                       $"Password={Env.GetString("DB_PASSWORD")};";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("SinfraPolicy", policy =>
    {
                policy.WithOrigins(
    "http://localhost:5000",
    "http://localhost:5049",
    "https://sinfra.nodesv.com",
    "https://sinfrapi.nodesv.com"  // <- minúscula, como lo pone Cloudflare
)
              .AllowAnyHeader()
              .AllowCredentials(); 
    });
});

// 4. Configuración de Autenticación con JWT vía Cookies
var jwtKey = Env.GetString("JWT_KEY");
var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Env.GetString("JWT_ISSUER"),
        ValidAudience = Env.GetString("JWT_AUDIENCE"),
        IssuerSigningKey = securityKey
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["X-Access-Token"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// 5. Inyección de Dependencias (Services)
builder.Services.AddScoped<IServerService, ServerService>();
builder.Services.AddScoped<IAuthService, AuthService>();  // <-- agrega este

// 6. Swagger con soporte de cookie HttpOnly
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SinfraRMM API", Version = "v1" });

    c.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = "X-Access-Token",
        Description = "Primero ejecuta /api/auth/login, la cookie se guarda automáticamente"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "cookieAuth"
                }
            },
            Array.Empty<string>()
        }
    });
     c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "ApiKey del agente (viene de la tabla servers)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});
var app = builder.Build();

// --- Middleware Pipeline ---

// 7. Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("SinfraPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<MonitorHub>("/hubs/monitor");
app.MapControllers();


app.Run();