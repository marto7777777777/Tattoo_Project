using Tattoo_Project.DTOs.AuthDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IEmailVerificationService
    {
        Task<ResultService> SendCodeAsync(ApplicationUser user, EmailVerificationPurpose purpose);

        Task<ResultService<AuthResponseDto>> VerifyRegisterCodeAsync(string email, string code);

        Task<ResultService> ResendRegisterCodeAsync(string email);

        Task<ResultService> SendForgotPasswordCodeAsync(string email);

        Task<ResultService> VerifyPasswordResetCodeAsync(string email, string code);

        Task<ResultService> ResetPasswordWithCodeAsync(string email, string code, string newPassword, string confirmNewPassword);

        Task<ResultService> SendPasswordChangeCodeAsync(string userId);

        Task<ResultService> ChangePasswordWithCodeAsync(string userId, string code, string newPassword, string confirmNewPassword);
    }
}
