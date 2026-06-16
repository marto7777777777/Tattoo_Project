using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITattooSessionService
    {
        Task<ResultService<ICollection<GetTattooSessionDto>>> GetAllTattooSessionsAsync();

        Task<ResultService<GetTattooSessionDto>> GetTattooSessionByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist);

        Task<ResultService> CreateTattooSessionAsync(
            CreateTattooSessionDto dto,
            string userId);

        Task<ResultService> UpdateTattooSessionAsync(
            int id,
            UpdateTattooSessionDto dto,
            string userId);

        Task<ResultService> DeleteTattooSessionAsync(
            int id,
            string userId);

        Task<ResultService> AddMoreSessionsAsync(
            int tattooRequestId,
            AddAdditionalSessionsDto dto,
            string userId);

        Task<ResultService> CompleteTattooAsync(
            int tattooRequestId,
            string userId);

        Task<ResultService> ContinueTattooAsync(int tattooRequestId);
    }
}