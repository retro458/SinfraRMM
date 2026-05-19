namespace SinfraRMM.Web.Dtos
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; } 
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Active";
    }
}