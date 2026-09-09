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
    Task<bool> HasAccessAsync(string userId);
}
