using Tattoo_Project.DTOs.ArtistUnavailableDateDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IArtistUnavailableDateService
    {
        Task<ResultService> CreateUnavailableDateAsync(CreateArtistUnavailableDateDto dto, string userId);

        Task<ResultService<ICollection<GetArtistUnavailableDateDto>>> GetMyUnavailableDatesAsync(string userId);

        Task<ResultService> DeleteUnavailableDateAsync(int id, string userId);
    }
}