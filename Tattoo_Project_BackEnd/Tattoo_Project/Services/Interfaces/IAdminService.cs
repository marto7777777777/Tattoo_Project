using Tattoo_Project.DTOs.AdminDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces;

public interface IAdminService
{
    Task<ResultService<AdminOverviewDto>> GetOverviewAsync();
    Task<ResultService<ICollection<AdminUserDto>>> GetUsersAsync();
    Task<ResultService<ICollection<AdminTattooRequestDto>>> GetTattooRequestsAsync();
    Task<ResultService<ICollection<AdminAiProjectDto>>> GetAiProjectsAsync();
    Task<ResultService> DeleteUserAsync(string userId, string currentAdminUserId);
    Task<ResultService> DeleteClientProfileAsync(int clientId);
    Task<ResultService> DeleteArtistProfileAsync(int artistId);
    Task<ResultService> DeleteTattooRequestAsync(int tattooRequestId);
    Task<ResultService> DeleteAiProjectAsync(int projectId);
    Task<ResultService> SetArtistVerifiedAsync(int artistId, bool isVerified);
}
