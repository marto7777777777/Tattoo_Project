using Tattoo_Project.DTOs.TattooRequestDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITattooRequestService
    {
        Task<ICollection<GetTattooRequestDto>> GetAllTattooRequestsAsync();

        Task<GetTattooRequestDto?> GetTattooRequestByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist);

        Task<ICollection<GetTattooRequestDto>> GetMyTattooRequestsAsync(
            string userId);

        Task<bool> CreateTattooRequest(
            CreateTattooRequestDto dto,
            string userId);

        Task<bool> UpdateTattooRequestAsync(
            int id,
            UpdateTattooRequestDto dto,
            string userId);
    }
}