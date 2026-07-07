using Tattoo_Project.DTOs.TattooArtistDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IClientFavoriteArtistService
    {
        Task<ResultService> AddFavoriteArtistAsync(int tattooArtistId, string userId);

        Task<ResultService> RemoveFavoriteArtistAsync(int tattooArtistId, string userId);

        Task<ResultService<ICollection<GetTattooArtistDto>>> GetMyFavoriteArtistsAsync(string userId);
    }
}