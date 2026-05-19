using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SinfraRMM.API.Models;

[Table("alert_rules")]
public partial class AlertRule
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("server_id")]
    public Guid? ServerId { get; set; }

    [Column("metric_name")]
    [StringLength(50)]
    public string? MetricName { get; set; }

    [Column("threshold")]
    [Precision(5, 2)]
    public decimal? Threshold { get; set; }

    [Column("operator")]
    [StringLength(5)]
    public string? Operator { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ServerId")]
    [InverseProperty("AlertRules")]
    public virtual Server? Server { get; set; }
}
