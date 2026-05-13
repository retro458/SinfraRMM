using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SinfraRMM.API.Models;

[Table("audit_logs")]
public partial class AuditLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("server_id")]
    public Guid? ServerId { get; set; }

    [Column("command_id")]
    public int? CommandId { get; set; }

    [Column("action_output")]
    public string? ActionOutput { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("CommandId")]
    [InverseProperty("AuditLogs")]
    public virtual CommandsLibrary? Command { get; set; }

    [ForeignKey("ServerId")]
    [InverseProperty("AuditLogs")]
    public virtual Server? Server { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("AuditLogs")]
    public virtual User? User { get; set; }
}
