using Tattoo_Project.DTOs.TattooSessionDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITattooSessionService
    {
        Task<ICollection<GetTattooSessionDto>> GetAllTattooSessionsAsync();

        Task<GetTattooSessionDto?> GetTattooSessionByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist);

        Task<bool> CreateTattooSessionAsync(
            CreateTattooSessionDto dto,
            string userId);

        Task<bool> UpdateTattooSessionAsync(
            int id,
            UpdateTattooSessionDto dto,
            string userId);

        Task<bool> DeleteTattooSessionAsync(
            int id,
            string userId);

        Task<bool> AddMoreSessionsAsync(
            int tattooRequestId,
            AddAdditionalSessionsDto dto,
            string userId);

        Task<bool> CompleteTattooAsync(
            int tattooRequestId,
            string userId);

        Task<bool> ContinueTattooAsync(int tattooRequestId);
    }
}