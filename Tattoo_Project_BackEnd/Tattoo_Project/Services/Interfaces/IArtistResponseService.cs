using Tattoo_Project.DTOs.ArtistResponseDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IArtistResponseService
    {
        Task<ResultService<ICollection<GetArtistResponseDto>>> GetAllArtistResponsesAsync();

        Task<ResultService<GetArtistResponseDto>> GetArtistResponseByIdAsync(
            int id,
            string userId);

        Task<ResultService<ICollection<GetArtistResponseDto>>> GetMyArtistResponsesAsync(
            string userId);

        Task<ResultService> CreateArtistResponseAsync(
            CreateArtistResponseDto dto,
            string userId);

        Task<ResultService> RejectTattooRequestAsync(
            int tattooRequestId,
            string userId);
    }
}