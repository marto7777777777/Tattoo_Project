using Tattoo_Project.DTOs.ArtistResponseDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IArtistResponseService
    {
        Task<List<GetArtistResponseDto>> GetAllArtistResponsesAsync();
        Task<GetArtistResponseDto> GetArtistResponseByIdAsync(int id);
        Task<bool> CreateArtistResponseAsync(CreateArtistResponseDto dto);
        Task<bool> DeleteArtistResponseAsync(int id);
        Task<bool> RejectTattooRequestAsync(int tattooRequestId);
    }
}
