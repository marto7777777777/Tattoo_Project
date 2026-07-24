using Tattoo_Project.DTOs.StudioDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IStudioService
    {
        Task<ResultService<ICollection<StudioDto>>> GetStudiosAsync(string? query = null);
        Task<ResultService<ICollection<StudioDto>>> SearchOpenStudiosForJoinAsync(string? query, string userId);
        Task<ResultService<StudioDto>> GetStudioByIdAsync(int studioId);
        Task<ResultService<MyStudioDto>> GetMyStudioAsync(string userId);
        Task<ResultService> CreateStudioForExistingArtistAsync(CreateStudioDto dto, string userId);
        Task<ResultService> RequestJoinAsync(int studioId, string userId);
        Task<ResultService> AcceptJoinRequestAsync(int requestId, string ownerUserId);
        Task<ResultService> RejectJoinRequestAsync(int requestId, string ownerUserId);
        Task<ResultService> RemoveMemberAsync(int artistId, string ownerUserId);
        Task<ResultService> SetOpenForJoinRequestsAsync(bool isOpen, string ownerUserId);
        Task<ResultService> UpdateStudioAsync(UpdateStudioDto dto, string ownerUserId);
    }
}
