
using SinfraRMM.API.Dtos;

namespace SinfraRMM.API.Interfaces
{
    public interface IServerService
    {
        Task<IEnumerable<ServerResponseDto>> GetAllAsync();
        Task<ServerResponseDto> GetByIdAsync(Guid id);
        Task<ServerResponseDto> CreateAsync(CreateServerDto dto);
        Task<ServerResponseDto> UpdateAsync(Guid id, UpdateServerDto dto);
        Task DeleteAsync(Guid id);
        Task<string> RegenerateApiKeyAsync(Guid id);
    }
}