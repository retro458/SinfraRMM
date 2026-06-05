using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SinfraRMM.API.Data;

namespace SinfraRMM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("general")]
        public async Task<IActionResult> GeneralReport(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to   = null)
        {
            // Defaults — último mes
            var dateFrom = from ?? DateTime.UtcNow.AddMonths(-1);
            var dateTo   = to   ?? DateTime.UtcNow;

            // Datos
            var servers = await _db.Servers
                .OrderBy(s => s.AssetCode)
                .ToListAsync();

            var metrics = await _db.MetricsHistories
                .Where(m => m.CreatedAt >= dateFrom && m.CreatedAt <= dateTo)
                .GroupBy(m => m.ServerId)
                .Select(g => new
                {
                    ServerId    = g.Key,
                    AvgCpu      = Math.Round(g.Average(m => m.CpuUsage) ?? 0m, 2),
                    AvgRam      = Math.Round(g.Average(m => m.RamUsage) ?? 0m, 2),
                    AvgDisk     = Math.Round(g.Average(m => m.DiskUsage) ?? 0m, 2),
                    MaxCpu      = g.Max(m => m.CpuUsage),
                    MaxRam      = g.Max(m => m.RamUsage),
                    TotalSamples = g.Count()
                })
                .ToListAsync();

            var auditLogs = await _db.AuditLogs
                .Include(a => a.Command)
                .Include(a => a.Server)
                .Include(a => a.User)
                .Where(a => a.CreatedAt >= dateFrom && a.CreatedAt <= dateTo)
                .OrderByDescending(a => a.CreatedAt)
                .Take(100)
                .ToListAsync();

            var alerts = await _db.AlertRules
                .Include(a => a.Server)
                .Where(a => a.IsActive)
                .ToListAsync();

            var topCommands = await _db.AuditLogs
                .Include(a => a.Command)
                .Where(a => a.CreatedAt >= dateFrom && a.CreatedAt <= dateTo)
                .GroupBy(a => a.Command!.Name)
                .Select(g => new { Command = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToListAsync();

            // Genera PDF
            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("SinfraRMM")
                                    .Bold().FontSize(22).FontColor("#1a1d27");
                                col.Item().Text("Reporte General del Sistema")
                                    .FontSize(12).FontColor("#4d9fff");
                                col.Item().Text($"Período: {dateFrom:dd/MM/yyyy} — {dateTo:dd/MM/yyyy}")
                                    .FontSize(9).FontColor("#6b7280");
                            });
                            row.ConstantItem(120).AlignRight().Column(col =>
                            {
                                col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor("#6b7280");
                                col.Item().Text($"Por: {User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value}")
                                    .FontSize(8).FontColor("#6b7280");
                            });
                        });
                    });

                    page.Content().Element(content =>
                    {
                        content.Column(col =>
                        {
                            // ── Resumen de servidores ──
                            col.Item().PaddingTop(20).Text("1. Inventario de Servidores")
                                .Bold().FontSize(13).FontColor("#1a1d27");

                            col.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(70);   // Código
                                    c.RelativeColumn(2);    // Nombre
                                    c.RelativeColumn(2);    // IP
                                    c.RelativeColumn(2);    // OS
                                    c.ConstantColumn(60);   // Status
                                });

                                // Header
                                table.Header(h =>
                                {
                                    foreach (var title in new[] { "Código", "Nombre", "IP", "OS", "Status" })
                                    {
                                        h.Cell().Background("#1a1d27").Padding(6)
                                            .Text(title).FontColor("#ffffff").Bold().FontSize(9);
                                    }
                                });

                                // Filas
                                foreach (var s in servers)
                                {
                                    var statusColor = s.Status == "Online" ? "#22c55e" : "#ef4444";
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(s.AssetCode).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(s.Name).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(s.IpAddress).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(s.OsInfo ?? "—").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(s.Status).FontColor(statusColor).Bold().FontSize(9);
                                }
                            });

                            // ── Métricas promedio ──
                            col.Item().PaddingTop(24).Text("2. Métricas Promedio del Período")
                                .Bold().FontSize(13).FontColor("#1a1d27");

                            col.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });

                                table.Header(h =>
                                {
                                    foreach (var t in new[] { "Servidor", "CPU Prom.", "RAM Prom.", "Disco Prom.", "CPU Máx.", "Muestras" })
                                    {
                                        h.Cell().Background("#1a1d27").Padding(6)
                                            .Text(t).FontColor("#ffffff").Bold().FontSize(9);
                                    }
                                });

                                foreach (var m in metrics)
                                {
                                    var server = servers.FirstOrDefault(s => s.Id == m.ServerId);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(server?.Name ?? "—").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text($"{m.AvgCpu}%").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text($"{m.AvgRam}%").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text($"{m.AvgDisk}%").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text($"{m.MaxCpu}%").FontColor("#ef4444").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text($"{m.TotalSamples}").FontSize(9);
                                }
                            });

                            // ── Top comandos ──
                            col.Item().PaddingTop(24).Text("3. Comandos Más Ejecutados")
                                .Bold().FontSize(13).FontColor("#1a1d27");

                            col.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(30);
                                    c.RelativeColumn(3);
                                    c.RelativeColumn();
                                });

                                table.Header(h =>
                                {
                                    foreach (var t in new[] { "#", "Comando", "Ejecuciones" })
                                    {
                                        h.Cell().Background("#1a1d27").Padding(6)
                                            .Text(t).FontColor("#ffffff").Bold().FontSize(9);
                                    }
                                });

                                var rank = 1;
                                foreach (var c in topCommands)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text($"{rank++}").FontSize(9).FontColor("#6b7280");
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(c.Command ?? "—").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text($"{c.Count}").Bold().FontSize(9).FontColor("#4d9fff");
                                }
                            });

                            // ── Reglas de alerta ──
                            col.Item().PaddingTop(24).Text("4. Reglas de Alerta Activas")
                                .Bold().FontSize(13).FontColor("#1a1d27");

                            col.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });

                                table.Header(h =>
                                {
                                    foreach (var t in new[] { "Servidor", "Métrica", "Condición", "Estado" })
                                    {
                                        h.Cell().Background("#1a1d27").Padding(6)
                                            .Text(t).FontColor("#ffffff").Bold().FontSize(9);
                                    }
                                });

                                foreach (var a in alerts)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(a.Server?.Name ?? "—").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(a.MetricName).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text($"{a.MetricName} {a.Operator} {a.Threshold}%").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(a.IsActive ? "Activa" : "Inactiva")
                                        .FontColor(a.IsActive ? "#22c55e" : "#6b7280").FontSize(9);
                                }
                            });

                            // ── Audit Log ──
                            col.Item().PaddingTop(24).Text("5. Historial de Auditoría (últimos 100 registros)")
                                .Bold().FontSize(13).FontColor("#1a1d27");

                            col.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(80);   // Fecha
                                    c.RelativeColumn();     // Servidor
                                    c.RelativeColumn(2);    // Comando
                                    c.RelativeColumn();     // Usuario
                                    c.ConstantColumn(50);   // Status
                                });

                                table.Header(h =>
                                {
                                    foreach (var t in new[] { "Fecha", "Servidor", "Comando", "Usuario", "Status" })
                                    {
                                        h.Cell().Background("#1a1d27").Padding(6)
                                            .Text(t).FontColor("#ffffff").Bold().FontSize(9);
                                    }
                                });

                                foreach (var log in auditLogs)
                                {
                                    var statusColor = log.Status == "Done" ? "#22c55e" : "#ef4444";
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(log.CreatedAt.HasValue ? log.CreatedAt.Value.ToString("dd/MM HH:mm") : "—").FontSize(8);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(log.Server?.Name ?? "—").FontSize(8);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(log.Command?.Name ?? "—").FontSize(8);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(log.User?.Email ?? "Sistema").FontSize(8);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                        .Text(log.Status).FontColor(statusColor).Bold().FontSize(8);
                                }
                            });
                        });
                    });

                    page.Footer().AlignCenter()
                        .Text(x =>
                        {
                            x.Span("SinfraRMM · Reporte generado automáticamente · Página ")
                                .FontSize(8).FontColor("#6b7280");
                            x.CurrentPageNumber().FontSize(8).FontColor("#6b7280");
                            x.Span(" de ").FontSize(8).FontColor("#6b7280");
                            x.TotalPages().FontSize(8).FontColor("#6b7280");
                        });
                });
            });

            var bytes = pdf.GeneratePdf();
            return File(bytes, "application/pdf",
                $"SinfraRMM-Reporte-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
        }
    }
}