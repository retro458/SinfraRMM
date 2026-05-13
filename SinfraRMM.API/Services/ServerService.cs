using Microsoft.EntityFrameworkCore;
using SinfraRMM.API.Data;
using SinfraRMM.API.Dtos;
using SinfraRMM.API.Interfaces;
using SinfraRMM.API.Models;

namespace SinfraRMM.API.Services
{
    public class ServerService : IServerService
    {
        private readonly AppDbContext _db;

        public ServerService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ServerResponseDto>> GetAllAsync()
        {
            return await _db.Servers
                .OrderBy(s => s.AssetCode)
                .Select(s => MapToDto(s))
                .ToListAsync();
        }

        public async Task<ServerResponseDto> GetByIdAsync(Guid id)
        {
            var server = await _db.Servers.FindAsync(id)
                ?? throw new KeyNotFoundException($"Servidor {id} no encontrado.");

            return MapToDto(server);
        }

        public async Task<ServerResponseDto> CreateAsync(CreateServerDto dto)
        {
            // Genera el asset code: SRV-FED-001, VPS-LIN-002, etc.
            var assetCode = await GenerateAssetCodeAsync(dto.Category);

            var server = new Server
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                IpAddress = dto.IpAddress,
                OsInfo = dto.OsInfo,
                Category = dto.Category.ToUpper(),
                AssetCode = assetCode,
                ApiKey = GenerateApiKey(),
                Status = "Offline",
                CreatedAt = DateTime.UtcNow
            };

            _db.Servers.Add(server);
            await _db.SaveChangesAsync();

            return MapToDto(server);
        }

        public async Task<ServerResponseDto> UpdateAsync(Guid id, UpdateServerDto dto)
        {
            var server = await _db.Servers.FindAsync(id)
                ?? throw new KeyNotFoundException($"Servidor {id} no encontrado.");

            // Solo actualiza los campos que vienen con valor
            if (dto.Name is not null) server.Name = dto.Name;
            if (dto.IpAddress is not null) server.IpAddress = dto.IpAddress;
            if (dto.OsInfo is not null) server.OsInfo = dto.OsInfo;
            if (dto.Status is not null)
            {
                var validStatuses = new[] { "Online", "Offline", "Maintenance" };
                if (!validStatuses.Contains(dto.Status))
                    throw new ArgumentException($"Status inválido. Valores permitidos: {string.Join(", ", validStatuses)}");

                server.Status = dto.Status;
            }

            await _db.SaveChangesAsync();
            return MapToDto(server);
        }

        public async Task DeleteAsync(Guid id)
        {
            var server = await _db.Servers.FindAsync(id)
                ?? throw new KeyNotFoundException($"Servidor {id} no encontrado.");

            _db.Servers.Remove(server);
            await _db.SaveChangesAsync();
        }

        // Regenera el ApiKey si se compromete la seguridad
        public async Task<string> RegenerateApiKeyAsync(Guid id)
        {
            var server = await _db.Servers.FindAsync(id)
                ?? throw new KeyNotFoundException($"Servidor {id} no encontrado.");

            server.ApiKey = GenerateApiKey();
            await _db.SaveChangesAsync();

            return server.ApiKey;
        }

        // Genera SRV-001, VPS-002, etc. contando los existentes de esa categoría
        private async Task<string> GenerateAssetCodeAsync(string category)
        {
            var cat = category.ToUpper();
            var count = await _db.Servers
                .CountAsync(s => s.Category == cat);

            return $"{cat}-{(count + 1):D3}"; // SRV-001, SRV-002...
        }

        // ApiKey segura: 32 bytes random en Base64 URL-safe
        private static string GenerateApiKey()
        {
            var bytes = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static ServerResponseDto MapToDto(Server s) => new()
        {
            Id = s.Id,
            AssetCode = s.AssetCode,
            Name = s.Name,
            IpAddress = s.IpAddress,
            OsInfo = s.OsInfo,
            Status = s.Status,
            Category = s.Category,
            ApiKey = s.ApiKey,
            LastHeartbeat = s.LastHeartbeat,
            CreatedAt = s.CreatedAt
        };
    }
}