using Tattoo_Project.DTOs.TattooArtistDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITattooArtistService
    {
        Task<ResultService<ICollection<GetTattooArtistDto>>> GetAllTattooArtistsAsync();

        Task<ResultService<GetTattooArtistDto>> GetTattooArtistByIdAsync(int id);

        Task<ResultService> CreateTattooArtistProfileAsync(
            CreateTattooArtistDto dto,
            string userId);

        Task<ResultService> UpdateTattooArtistProfileAsync(
            UpdateArtistDto dto,
            string userId);

        Task<ResultService> DeleteTattooArtistAsync(int id);

        Task<ResultService<ICollection<GetTattooArtistDto>>> SearchTattooArtistsAsync(string query);
    }
}