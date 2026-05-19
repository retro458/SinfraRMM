using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SinfraRMM.API.Models;

[Table("commands_library")]
public partial class CommandsLibrary
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("actual_command")]
    public string ActualCommand { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [InverseProperty("Command")]

    [Column("requires_admin")
    ]
    public bool RequiresAdmin { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<CommandQueue> CommandQueues { get; set; } = new List<CommandQueue>();
}
