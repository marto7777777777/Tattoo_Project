using Tattoo_Project.DTOs.TattooSessionDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITattooSessionService
    {
        Task<List<GetTattooSessionDto>> GetAllTattooSessions();
        Task<GetTattooSessionDto> GetTattooSessionById(int id);
        Task<bool> CreateTattooSession(CreateTattooSessionDto dto);
        Task<bool> UpdateTattooSession(int id, UpdateTattooSessionDto dto);
        Task<bool> DeleteTattooSession(int id);

    }
}
