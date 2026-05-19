using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SinfraRMM.API.Models;

[Table("servers")]
[Index("ApiKey", Name = "servers_api_key_key", IsUnique = true)]
public partial class Server
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("ip_address")]
    [StringLength(100)]
    public string? IpAddress { get; set; }

    [Column("os_info")]
    public string? OsInfo { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("api_key")]
    public string ApiKey { get; set; } = null!;

    [Column("last_heartbeat")]
    public DateTime? LastHeartbeat { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    [Column("category")]
    [StringLength(50)]
    public string? Category { get; set; }
    [Column("asset_code")]
    [StringLength(50)]
    public string? AssetCode { get; set; }
    [InverseProperty("Server")]
    public virtual ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();

    [InverseProperty("Server")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [InverseProperty("Server")]
    public virtual ICollection<MetricsHistory> MetricsHistories { get; set; } = new List<MetricsHistory>();
    public virtual ICollection<CommandQueue> CommandQueues { get; set; } = new List<CommandQueue>();
}
