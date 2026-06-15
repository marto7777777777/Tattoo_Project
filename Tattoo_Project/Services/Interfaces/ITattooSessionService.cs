using Tattoo_Project.DTOs.TattooSessionDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITattooSessionService
    {
        Task<List<GetTattooSessionDto>> GetAllTattooSessionsAsync();
        Task<GetTattooSessionDto> GetTattooSessionByIdAsync(int id);
        Task<bool> CreateTattooSessionAsync(CreateTattooSessionDto dto, string userId);
        Task<bool> UpdateTattooSessionAsync(int id, UpdateTattooSessionDto dto);
        Task<bool> DeleteTattooSessionAsync(int id);
        Task<bool> CompleteTattooAsync(int tattooRequestId, string userId);
        Task<bool> ContinueTattooAsync(int tattooRequestId);
        Task<bool> AddMoreSessionsAsync(int tattooRequestId, AddAdditionalSessionsDto dto, string userId);
    }
}
