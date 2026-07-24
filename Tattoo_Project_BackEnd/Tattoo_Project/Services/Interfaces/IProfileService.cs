using Microsoft.AspNetCore.Http;
using Tattoo_Project.DTOs.ProfileDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IProfileService
    {
        Task<ResultService<CurrentProfileDto>> GetMyProfileAsync(string userId);

        Task<ResultService> UpdateFirstNameAsync(string userId, string value);
        Task<ResultService> UpdateLastNameAsync(string userId, string value);
        Task<ResultService> UpdateEmailAsync(string userId, string value);

        Task<ResultService<string>> UpdateProfileImageAsync(string userId, IFormFile image);
        Task<ResultService> UpdatePhoneNumberAsync(string userId, string value);
        Task<ResultService> UpdateCityAsync(string userId, string value);
        Task<ResultService> UpdateCountryAsync(string userId, string value);

        Task<ResultService> UpdateDescriptionAsync(string userId, string value);

        Task<ResultService> UpdateConsultationDurationAsync(string userId, int value);
        Task<ResultService> UpdateOffersOnlineConsultationAsync(string userId, bool value);
        Task<ResultService> UpdateRequiresDepositAsync(string userId, bool value);
        Task<ResultService> UpdateDepositAmountAsync(string userId, decimal? value);

        Task<ResultService<ProfileRequirementDto>> AddRequirementAsync(string userId, string description);
        Task<ResultService> UpdateRequirementAsync(string userId, int requirementId, string description);
        Task<ResultService> DeleteRequirementAsync(string userId, int requirementId);

        Task<ResultService<ProfilePortfolioImageDto>> AddPortfolioImageAsync(string userId, IFormFile image);
        Task<ResultService> DeletePortfolioImageAsync(string userId, int imageId);
    }
}
