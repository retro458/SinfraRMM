# SinfraRMM

Sistema de Gestión y Monitoreo Remoto (RMM) desarrollado con **.NET 9**, **PostgreSQL** y un agente Python para servidores Linux. Permite monitorear métricas en tiempo real, ejecutar comandos remotos con whitelist de seguridad y gestionar usuarios con roles diferenciados.

---

## ¿Qué hace?

- **Monitoreo en tiempo real** — CPU, RAM y disco de cada servidor vía SignalR
- **Consola de comandos remota** — ejecución de comandos predefinidos (whitelist) en el kernel de Linux
- **Gestión de activos** — inventario de servidores con código único (SVR-001, DB-001, etc.)
- **Sistema de alertas** — notificaciones automáticas cuando una métrica supera un umbral configurado
- **Audit Log** — registro completo de cada acción: usuario, comando, resultado y timestamp
- **Gestión de usuarios y roles** — Admin, Técnico y Auditor con permisos diferenciados
- **Autenticación dual** — login local con BCrypt + OAuth 2.0 con Google
- **Aprobación de usuarios** — los técnicos que entran por OAuth quedan pendientes hasta que el admin aprueba

---

## Arquitectura

```
┌─────────────────────────────────────────────────────────────────┐
│                         Cliente (Browser)                        │
│                     ASP.NET Core MVC (.NET 9)                   │
│               Bootstrap · AJAX · SignalR Client                  │
└────────────────────────────┬────────────────────────────────────┘
                             │ HTTP + Cookie JWT
                             │ WebSocket (SignalR)
┌────────────────────────────▼────────────────────────────────────┐
│                      SinfraRMM.API                              │
│                  ASP.NET Core Web API (.NET 9)                  │
│         JWT Auth · SignalR Hub · EF Core · BCrypt               │
└──────────────┬──────────────────────────────┬───────────────────┘
               │ Npgsql                        │ Tailscale (VPN)
               │                              │ X-Api-Key
┌──────────────▼──────────┐      ┌────────────▼───────────────────┐
│      PostgreSQL          │      │     Agente Python (Fedora)     │
│  users · servers        │      │  psutil · subprocess · requests │
│  metrics_history        │      │  systemd service               │
│  audit_logs · alerts    │      │  polling cada 5s               │
└─────────────────────────┘      └────────────────────────────────┘
```

### Flujo de monitoreo

```
Agente → POST /api/agent/heartbeat    → status Online + last_heartbeat
Agente → POST /api/agent/metrics      → metrics_history + SignalR → MVC
Agente → GET  /api/agent/pending-commands → ejecuta → POST /api/agent/command-result → audit_log + SignalR
```

### Flujo de comandos

```
Técnico (MVC) → POST /api/commands/execute → command_queue (Pending)
Agente polling → GET /api/agent/pending-commands → ejecuta con subprocess
Agente → POST /api/agent/command-result → command_queue (Done/Error) + audit_log
API → SignalR → MVC muestra output en consola
```

---

## Tecnologías

| Capa | Tecnología |
|------|-----------|
| Backend API | ASP.NET Core Web API .NET 9 |
| Frontend | ASP.NET Core MVC .NET 9 |
| Base de datos | PostgreSQL 16 |
| ORM | Entity Framework Core 9 + Npgsql |
| Tiempo real | SignalR |
| Autenticación | JWT Bearer + Cookie HttpOnly |
| OAuth | Google OAuth 2.0 |
| Hash de contraseñas | BCrypt.Net |
| Variables de entorno | DotNetEnv |
| Agente | Python 3 + psutil + requests |
| VPN | Tailscale |
| Servidor Linux | Fedora Linux 44 |

---

## Estructura del proyecto

```
SinfraRMM/
├── SinfraRMM.API/
│   ├── Controllers/        # Endpoints REST
│   │   ├── AuthController.cs
│   │   ├── AgentController.cs
│   │   ├── ServersController.cs
│   │   ├── CommandsController.cs
│   │   ├── AlertRulesController.cs
│   │   ├── AuditLogsController.cs
│   │   └── UsersController.cs
│   ├── Services/           # Lógica de negocio
│   │   ├── AuthService.cs
│   │   └── ServerService.cs
│   ├── Interfaces/         # Contratos de servicios
│   ├── Models/             # Entidades EF Core
│   ├── Dtos/               # Objetos de transferencia
│   ├── Hubs/               # SignalR MonitorHub
│   └── Data/               # AppDbContext
│
├── SinfraRMM.Web/
│   ├── Controllers/        # Controladores MVC
│   ├── Views/              # Vistas Razor
│   │   ├── Auth/           # Login
│   │   ├── Dashboard/      # Panel principal
│   │   ├── Servers/        # Inventario + Consola
│   │   ├── Commands/       # Whitelist de comandos
│   │   ├── Alerts/         # Reglas de alerta
│   │   ├── Users/          # Gestión de usuarios
│   │   └── AuditLogs/      # Historial de auditoría
│   ├── Services/
│   │   └── ApiClient.cs    # HttpClient wrapper
│   └── Models/             # ViewModels + DTOs
│
└── agente/
    └── agente.py           # Agente para servidor Fedora
```

---

## Requisitos

- .NET 9 SDK
- PostgreSQL 16+
- Python 3.10+
- pip: `psutil`, `requests`
- Tailscale (para conectar API con el servidor remoto)

---

## Configuración

### 1. Base de datos

Ejecuta los scripts SQL en orden para crear las tablas:

```sql
-- roles, users, servers, commands_library,
-- metrics_history, audit_logs, alert_rules,
-- command_queue, notifications
```

Inserta los roles base:

```sql
INSERT INTO roles (name) VALUES ('Admin'), ('Tecnico'), ('Auditor');
```

### 2. Variables de entorno — API (`SinfraRMM.API/.env`)

```env
DB_HOST=IP_TAILSCALE_DEL_SERVER
DB_PORT=5432
DB_NAME=nombre_db
DB_USER=usuario_db
DB_PASSWORD=password_db

JWT_KEY=clave_secreta_minimo_32_caracteres
JWT_ISSUER=SinfraAPI
JWT_AUDIENCE=SinfraWeb

ADMIN_EMAIL=admin@tudominio.com
ADMIN_PASSWORD=PasswordSeguro123!
```

### 3. Variables de entorno — MVC (`SinfraRMM.Web/.env`)

```env
API_URL=http://IP_TAILSCALE_API:5281

GOOGLE_CLIENT_ID=client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=client-secret
```

### 4. Agente Python

```bash
pip install psutil requests
```

 `agente/agente.py`:

```python
API_URL = "http://IP_TAILSCALE_API:5281"

SERVERS = [
    {
        "name":    "Nombre del servidor",
        "api_key": "api_key_generada_al_registrar_servidor"
    }
]
```

Registra como servicio systemd:

```bash
sudo cp agente.py /opt/sinfra/agente.py
sudo cp sinfra-agent.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable sinfra-agent
sudo systemctl start sinfra-agent
```

### 5. Levantar en desarrollo

```bash
# Terminal 1 — API
cd SinfraRMM.API
dotnet run

# Terminal 2 — MVC
cd SinfraRMM.Web
dotnet run

# En el servidor Fedora — agente
sudo systemctl start sinfra-agent
sudo journalctl -u sinfra-agent -f
```

---

## Roles y permisos

| Acción | Admin | Técnico | Auditor |
|--------|-------|---------|---------|
| Ver dashboard y métricas | ✅ | ✅ | ✅ |
| Ver servidores | ✅ | ✅ | ✅ |
| Ejecutar comandos | ✅ | ✅ | ❌ |
| Ejecutar comandos Admin | ✅ | ❌ | ❌ |
| Registrar servidores | ✅ | ❌ | ❌ |
| Gestionar comandos whitelist | ✅ | ❌ | ❌ |
| Configurar alertas | ✅ | ❌ | ❌ |
| Ver audit log | ✅ | ❌ | ✅ |
| Gestionar usuarios | ✅ | ❌ | ❌ |
| Aprobar usuarios OAuth | ✅ | ❌ | ❌ |

---

## Seguridad

- **Whitelist de comandos** — el agente solo ejecuta comandos registrados en `commands_library`, nunca strings libres
- **ApiKey por servidor** — cada servidor tiene una clave única para identificarse ante la API
- **JWT HttpOnly** — el token viaja en cookie HttpOnly, nunca en localStorage
- **Roles en claims** — cada endpoint valida el rol del usuario via `[Authorize(Roles = "Admin")]`
- **Aprobación manual** — los usuarios que entran por OAuth quedan en estado `Pending` hasta aprobación del admin
- **BCrypt** — las contraseñas locales se almacenan con hash bcrypt, nunca en texto plano

---

## Equipo

| Nombre | Carnet |
|--------|--------|
| Erick Abraham Baudriz Monge | 215823 |
| Raúl Alexander Campos García | 127023 |
| Edgar Ulises López Gutiérrez | 224523 |
| Brayan Ernesto Cruz Aldana | 260522 |

---

*Proyecto universitario — Desarrollo Web Avanzado · 2026*