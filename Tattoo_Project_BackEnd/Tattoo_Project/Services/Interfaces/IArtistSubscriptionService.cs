using Stripe;
using Tattoo_Project.DTOs.SubscriptionDTOs;
using Tattoo_Project.Services.Results;
namespace Tattoo_Project.Services.Interfaces;
public interface IArtistSubscriptionService
{
    Task<ResultService<SubscriptionStatusDto>> GetStatusAsync(string userId);
    Task<ResultService<StripeRedirectDto>> CreateCheckoutAsync(string userId);
    Task<ResultService<StripeRedirectDto>> CreatePortalAsync(string userId);
    Task<ResultService> ProcessVerifiedEventAsync(Event stripeEvent);
    Task<ResultService> ApplyVerifiedProviderAsync(string? userId,VerifiedProviderSubscription state);
    Task<ResultService<MobileBillingContextDto>> GetMobileContextAsync(string userId,string provider);
    Task<ResultService<MobileBillingContextDto>> GetAiMobileContextAsync(string userId,string provider);
    Task<bool> HasAccessAsync(string userId);
}
