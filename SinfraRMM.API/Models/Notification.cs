namespace SinfraRMM.API.Models
{
    public class Notification
    {
        public long Id { get; set; }
        public string Type { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public string? Data { get; set; } // JSON string
        public DateTime CreatedAt { get; set; }
    }
}