using Microsoft.AspNetCore.Http;
using Tattoo_Project.DTOs;
using Tattoo_Project.DTOs.AdminDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces;

public interface IArtistModerationService
{
    Task<ResultService> CreateReportAsync(string reporterUserId, int artistId, CreateArtistReportDto dto);
    Task<ResultService<ArtistModerationStatusDto>> GetMyStatusAsync(string userId);
    Task<ResultService<ProfileModerationUploadDto>> AddReinstatementImageAsync(string userId, IFormFile image);
    Task<ResultService> DeleteReinstatementImageAsync(string userId, int imageId);
    Task<ResultService> SubmitReinstatementAsync(string userId);
    Task<ResultService<ICollection<AdminArtistReportDto>>> GetReportsAsync();
    Task<ResultService<ICollection<AdminModerationSubmissionDto>>> GetSubmissionsAsync();
    Task<ResultService> ReviewReportAsync(int reportId, string adminUserId, ModerateArtistReportRequest request);
    Task<ResultService> ReviewSubmissionAsync(int submissionId, string adminUserId, ModerateArtistSubmissionRequest request);
}

public sealed class ProfileModerationUploadDto
{
    public int SubmissionId { get; set; }
    public int ImageId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
