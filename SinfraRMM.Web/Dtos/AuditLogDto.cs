namespace SinfraRMM.Web.Dtos
{
    public class AuditLogDto
    {
        public long Id { get; set; }
        public string Server { get; set; } = string.Empty;
        public string AssetCode { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string? ActionOutput { get; set; }
        public string Status { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    public class AuditLogPagedDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<AuditLogDto> Logs { get; set; } = new();
    }
}