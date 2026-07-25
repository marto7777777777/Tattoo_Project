using Tattoo_Project.DTOs.StudioDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IClientFavoriteStudioService
    {
        Task<ResultService> AddAsync(int studioId, string userId);
        Task<ResultService> RemoveAsync(int studioId, string userId);
        Task<ResultService<ICollection<StudioDto>>> GetMineAsync(string userId);
    }
}
