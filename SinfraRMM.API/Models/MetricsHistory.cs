using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SinfraRMM.API.Models;

[Table("metrics_history")]
public partial class MetricsHistory
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("server_id")]
    public Guid? ServerId { get; set; }

    [Column("cpu_usage")]
    [Precision(5, 2)]
    public decimal? CpuUsage { get; set; }

    [Column("ram_usage")]
    [Precision(5, 2)]
    public decimal? RamUsage { get; set; }

    [Column("disk_usage")]
    [Precision(5, 2)]
    public decimal? DiskUsage { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ServerId")]
    [InverseProperty("MetricsHistories")]
    public virtual Server? Server { get; set; }
}
