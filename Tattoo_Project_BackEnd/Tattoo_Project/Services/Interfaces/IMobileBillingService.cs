using Tattoo_Project.DTOs.SubscriptionDTOs;using Tattoo_Project.Services.Results;
namespace Tattoo_Project.Services.Interfaces;
public interface IMobileBillingService
{
 Task<ResultService<MobilePurchaseVerificationResultDto>> VerifyAsync(string userId,MobilePurchaseVerificationDto dto);
 Task<ResultService> ProcessGoogleNotificationAsync(string payload,string authorization,string expectedAudience);
 Task<ResultService> ProcessAppleNotificationAsync(string signedPayload);
}
