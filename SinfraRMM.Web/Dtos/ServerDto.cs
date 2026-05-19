namespace SinfraRMM.Web.Dtos
{
   public class ServerDto
    {
         public Guid Id { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? OsInfo { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt {get;set;}
        public DateTime? LastHeartbeat { get; set; }


    }

}