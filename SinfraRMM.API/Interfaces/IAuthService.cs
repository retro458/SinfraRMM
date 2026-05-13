namespace SinfraRMM.API.Interfaces
{
    using SinfraRMM.API.Dtos;

    public interface IAuthService
    {
        Task<(AuthResponseDto dto, string token)> LoginAsync(LoginDto request);
        Task<(AuthResponseDto dto, string token)> RegisterAsync(RegisterDto request);
        Task<(AuthResponseDto dto, string token)> ExternalLoginAsync(ExternalLoginDto request);
    }
}