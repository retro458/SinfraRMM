using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SinfraRMM.API.Models;

namespace SinfraRMM.API.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AlertRule> AlertRules { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CommandsLibrary> CommandsLibraries { get; set; }

    public virtual DbSet<MetricsHistory> MetricsHistories { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Server> Servers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<CommandQueue> CommandQueue { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlertRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("alert_rules_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Server).WithMany(p => p.AlertRules)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("alert_rules_server_id_fkey");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Command).WithMany(p => p.AuditLogs).HasConstraintName("audit_logs_command_id_fkey");

            entity.HasOne(d => d.Server).WithMany(p => p.AuditLogs)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("audit_logs_server_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("audit_logs_user_id_fkey");
        });

      modelBuilder.Entity<CommandQueue>(entity =>
 {
    // 1. Clave primaria autoincremental (Correcto)
    entity.HasKey(e => e.Id).HasName("command_queue_pkey");
    entity.Property(e => e.Id).UseIdentityAlwaysColumn();

    entity.Property(e => e.created_at).HasDefaultValueSql("now()");

    // 2. Relación con la clase Command
    entity.HasOne(d => d.Command) // Propiedad de navegación en CommandQueue
          .WithMany(p => p.CommandQueues) // Propiedad de colección en Command
          .HasForeignKey(d => d.CommandId) 
          .OnDelete(DeleteBehavior.Cascade)
          .HasConstraintName("command_queue_command_id_fkey");

    // 3. Relación con la clase Server (Corrección del error)
    entity.HasOne(d => d.Server) // Propiedad de navegación en CommandQueue
          .WithMany(p => p.CommandQueues) // Propiedad de colección en Server
          .HasForeignKey(d => d.ServerId) 
          .HasConstraintName("command_queue_server_id_fkey");

    // 4. Relación con la clase User
    entity.HasOne(d => d.User) // Propiedad de navegación en CommandQueue
          .WithMany(p => p.CommandQueues) // Propiedad de colección en User
          .HasForeignKey(d => d.RequestedBy) 
          .OnDelete(DeleteBehavior.SetNull)
          .HasConstraintName("command_queue_requested_by_fkey");
 });

        modelBuilder.Entity<CommandsLibrary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("commands_library_pkey");
            entity.Property(c => c.Category).HasColumnName("category").HasMaxLength(50);
            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.RequiresAdmin).HasDefaultValue(false);
        });

        modelBuilder.Entity<MetricsHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("metrics_history_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Server).WithMany(p => p.MetricsHistories)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("metrics_history_server_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        });

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("servers_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Status).HasDefaultValueSql("'Offline'::character varying");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");
            entity.Property(u => u.password).HasColumnName("password").HasMaxLength(200).IsRequired(false);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(u => u.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Active");

            entity.HasOne(d => d.Role).WithMany(p => p.Users).HasConstraintName("users_role_id_fkey");
        });
        modelBuilder.Entity<Notification>(e =>
            {
            e.ToTable("notifications");
            e.HasKey(n => n.Id);
            e.Property(n => n.Id).HasColumnName("id");
            e.Property(n => n.Type).HasColumnName("type");
            e.Property(n => n.Message).HasColumnName("message");
            e.Property(n => n.IsRead).HasColumnName("is_read");
            e.Property(n => n.Data).HasColumnName("data");
            e.Property(n => n.CreatedAt).HasColumnName("created_at");
            });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
