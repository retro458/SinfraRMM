using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SinfraRMM.API.Models;

[Table("command_queue")]
public partial class CommandQueue
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("server_id")]
    public Guid ServerId { get; set; }
    [Column("command_id")]
    public int CommandId { get; set; }
    [Column("requested_by")]
    public Guid RequestedBy { get; set; }
    [Column("status")]    
    public string? status { get; set; }
    [Column("created_at")]
    public DateTime created_at { get; set; }
    [Column("executed_at")]
    public DateTime executed_at { get; set; }

    public virtual CommandsLibrary Command { get; set; } = null!;
    public virtual Server Server { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}