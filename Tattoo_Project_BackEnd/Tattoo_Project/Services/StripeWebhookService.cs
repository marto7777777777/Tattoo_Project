using Stripe;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public class StripeWebhookService(
    IConfiguration config,
    IArtistSubscriptionService subscriptions,
    IAiTattooService ai,
    IProviderWebhookEventService events) : IStripeWebhookService
{
    public async Task<ResultService> ProcessAsync(string payload, string signature)
    {
        var secret = config["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            return ResultService.Fail("Stripe webhook is not configured.");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, secret, 300, false);
        }
        catch (StripeException)
        {
            return ResultService.Fail("Invalid Stripe signature.");
        }

        return await events.ExecuteOnceAsync(
            SubscriptionProviders.Stripe,
            stripeEvent.Id,
            stripeEvent.Type,
            payload,
            async () =>
            {
                if (stripeEvent.Type == "checkout.session.completed" &&
                    stripeEvent.Data.Object is Stripe.Checkout.Session session)
                {
                    var purpose = session.Metadata?.GetValueOrDefault("purpose");
                    return purpose switch
                    {
                        "artist_subscription" => await subscriptions.ProcessVerifiedEventAsync(stripeEvent),
                        "ai_project_pass" => await ai.ProcessVerifiedStripeEventAsync(stripeEvent),
                        _ => ResultService.Fail("Stripe checkout purpose metadata is invalid.")
                    };
                }

                return await subscriptions.ProcessVerifiedEventAsync(stripeEvent);
            });
    }
}
