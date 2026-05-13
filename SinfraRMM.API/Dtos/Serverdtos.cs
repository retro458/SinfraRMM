// Dtos/ServerDtos.cs
using System.ComponentModel.DataAnnotations;

namespace SinfraRMM.API.Dtos
{
    // =====================================
    // REQUESTS
    // =====================================

    public class CreateServerDto
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string IpAddress { get; set; } = null!;

        public string? OsInfo { get; set; }

        [Required]
        public string Category { get; set; } = null!; // 'SRV', 'VPS', 'DB', etc.
    }

    public class UpdateServerDto
    {
        public string? Name { get; set; }
        public string? IpAddress { get; set; }
        public string? OsInfo { get; set; }
        public string? Status { get; set; } // 'Online', 'Offline', 'Maintenance'
    }

    // =====================================
    // RESPONSES
    // =====================================

    public class ServerResponseDto
    {
        public Guid Id { get; set; }
        public string AssetCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string IpAddress { get; set; } = null!;
        public string? OsInfo { get; set; }
        public string Status { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public DateTime? LastHeartbeat { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}