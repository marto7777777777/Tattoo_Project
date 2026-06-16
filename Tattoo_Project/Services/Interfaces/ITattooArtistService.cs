using Tattoo_Project.DTOs.TattooArtistDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITattooArtistService
    {
        Task<ICollection<GetTattooArtistDto>> GetAllTattooArtistsAsync();

        Task<GetTattooArtistDto?> GetTattooArtistByIdAsync(int id);

        Task<bool> CreateTattooArtistProfileAsync(
            CreateTattooArtistDto dto,
            string userId);

        Task<bool> UpdateTattooArtistProfileAsync(
            UpdateArtistDto dto,
            string userId);

        Task<bool> DeleteTattooArtistAsync(int id);
    }
}