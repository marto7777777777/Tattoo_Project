using Tattoo_Project.DTOs.TattooSessionDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITattooSessionService
    {
        Task<List<GetTattooSessionDto>> GetAllTattooSessionsAsync();
        Task<GetTattooSessionDto> GetTattooSessionByIdAsync(int id);
        Task<bool> CreateTattooSessionAsync(CreateTattooSessionDto dto);
        Task<bool> UpdateTattooSessionAsync(int id, UpdateTattooSessionDto dto);
        Task<bool> DeleteTattooSessionAsync(int id);

    }
}
