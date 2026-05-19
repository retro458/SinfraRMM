namespace SinfraRMM.Web.Models.Auth;

public class AuthDataDto
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public String? AvatarUrl { get; set; }

}