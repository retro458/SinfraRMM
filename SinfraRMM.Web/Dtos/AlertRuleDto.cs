namespace SinfraRMM.Web.Dtos
{
    public class AlertRuleDto
    {
        public int Id { get; set; }
        public Guid ServerId { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public string AssetCode { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public decimal Threshold { get; set; }
        public string Operator { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}