using Tattoo_Project.DTOs.AiTattooDTOs;
using Tattoo_Project.Services.Results;
namespace Tattoo_Project.Services.Interfaces;
public interface IAiTattooService
{
 Task<ResultService<AiTattooProjectDto>> CreatePaidDraftAsync(CreateAiTattooProjectDto dto,string userId);
 Task<ResultService<AiTattooProjectDto>> GenerateInitialAsync(int id,string userId);
 Task<ResultService<AiTattooProjectDto>> CreateProjectAsync(CreateAiTattooProjectDto dto,string userId);
 Task<ResultService<ICollection<AiTattooProjectDto>>> GetMyProjectsAsync(string userId);
 Task<ResultService<AiTattooProjectDto>> GetProjectAsync(int id,string userId);
 Task<ResultService<AiTattooProjectDto>> EditProjectAsync(int id,EditAiTattooProjectDto dto,string userId);
 Task<ResultService<CheckoutSessionDto>> CreateCheckoutAsync(int projectId,string userId);
 Task<ResultService> ProcessStripeWebhookAsync(string payload,string signature);
}
