using Tattoo_Project.DTOs;
using Tattoo_Project.Models;

namespace Tattoo_Project.Services
{
    public interface ITattooArtistService
    {
        Task<List<TattooArtistDto>> GetAllArtistsAsync();
        Task<TattooArtistDto> GetTattooArtistByIdAsync(int Id);
    }
}
