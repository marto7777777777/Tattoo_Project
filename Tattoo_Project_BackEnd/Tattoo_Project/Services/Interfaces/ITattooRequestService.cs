using Tattoo_Project.DTOs.TattooRequestDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITattooRequestService
    {
        Task<ResultService<ICollection<GetTattooRequestDto>>> GetAllTattooRequestsAsync();

        Task<ResultService<GetTattooRequestDto>> GetTattooRequestByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist);

        Task<ResultService<ICollection<GetTattooRequestDto>>> GetMyTattooRequestsAsync(
            string userId);

        Task<ResultService<ICollection<GetTattooRequestDto>>> GetMyArtistTattooRequestsAsync(
            string userId,
            RequestStatus? status);

        Task<ResultService> CreateTattooRequestAsync(
            CreateTattooRequestDto dto,
            string userId);

        Task<ResultService> UpdateTattooRequestAsync(
            int id,
            UpdateTattooRequestDto dto,
            string userId);
    }
}