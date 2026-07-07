using Tattoo_Project.DTOs.ArtistReviewDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IArtistReviewService
    {
        Task<ResultService> CreateArtistReviewAsync(CreateArtistReviewDto dto, string userId);

        Task<ResultService<ICollection<GetArtistReviewDto>>> GetArtistReviewsByArtistIdAsync(int tattooArtistId);
    }
}
