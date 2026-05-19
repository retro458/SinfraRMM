namespace SinfraRMM.API.Dtos
{
    public class HeartbeatDto
    {
        public string IpAddress { get; set; } = string.Empty;
        public string OsInfo { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        
    }

    public class MetricDto
    {
        public decimal CpuUsage { get; set; }
        public decimal RamUsage { get; set; }
        public decimal DiskUsage { get; set; }
    }

    public class CommandResultDto
    {
        public string Output { get; set; } = string.Empty;
        public string? Status { get; set; } 
        public int QueueId { get; set; }
    }

    public class ExecuteCommandDto
{
    public Guid ServerId { get; set; }
    public int CommandId { get; set; }
}
}