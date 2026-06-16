using Tattoo_Project.DTOs.ArtistResponceDTOs;
using Tattoo_Project.DTOs.ArtistResponseDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IArtistResponseService
    {
        Task<ICollection<GetArtistResponseDto>> GetAllArtistResponsesAsync();

        Task<GetArtistResponseDto?> GetArtistResponseByIdAsync(
            int id,
            string userId);

        Task<ICollection<GetArtistResponseDto>> GetMyArtistResponsesAsync(string userId);

        Task<bool> CreateArtistResponseAsync(
            CreateArtistResponseDto dto,
            string userId);

        Task<bool> RejectTattooRequestAsync(
            int tattooRequestId,
            string userId);
    }
}
