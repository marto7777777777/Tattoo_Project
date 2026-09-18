using Tattoo_Project.Models;

namespace Tattoo_Project.Services;

public static class SubscriptionEntitlementRules
{
    public static string Normalize(
        string status,
        DateTime? trialEndsAt,
        DateTime? currentPeriodEndsAt,
        bool cancelAtPeriodEnd,
        bool isTrial,
        DateTime now)
    {
        if (status == ArtistSubscriptionStatuses.Cancelled)
        {
            if (currentPeriodEndsAt > now)
                return isTrial ? ArtistSubscriptionStatuses.Trialing : ArtistSubscriptionStatuses.Active;
            return ArtistSubscriptionStatuses.Expired;
        }

        if (status == ArtistSubscriptionStatuses.Trialing && trialEndsAt <= now)
            return ArtistSubscriptionStatuses.Expired;

        if (status == ArtistSubscriptionStatuses.GracePeriod && currentPeriodEndsAt <= now)
            return ArtistSubscriptionStatuses.Expired;

        if (status == ArtistSubscriptionStatuses.Active && currentPeriodEndsAt <= now)
            return ArtistSubscriptionStatuses.Expired;

        // on_hold, paused, past_due and revoked must never be upgraded merely
        // because the provider also reports a future date or cancellation flag.
        return status;
    }

    public static bool HasAccess(ArtistSubscription subscription, DateTime now) =>
        subscription.Status switch
        {
            ArtistSubscriptionStatuses.Trialing => subscription.TrialEndsAt > now,
            ArtistSubscriptionStatuses.GracePeriod => subscription.CurrentPeriodEndsAt > now,
            ArtistSubscriptionStatuses.Active => subscription.CurrentPeriodEndsAt > now,
            _ => false
        };

    public static bool ProviderHasAccess(ProviderSubscription subscription, DateTime now)
    {
        var normalized = Normalize(
            subscription.Status,
            subscription.TrialEndsAt,
            subscription.CurrentPeriodEndsAt,
            subscription.CancelAtPeriodEnd,
            subscription.Status == ArtistSubscriptionStatuses.Trialing,
            now);
        return ArtistSubscriptionStatuses.GrantsAccess(normalized);
    }

    public static IQueryable<TattooArtist> WithActiveEntitlement(
        this IQueryable<TattooArtist> query,
        DateTime now) => query.Where(artist =>
            artist.Subscription != null &&
            ((artist.Subscription.Status == ArtistSubscriptionStatuses.Trialing && artist.Subscription.TrialEndsAt > now) ||
             (artist.Subscription.Status == ArtistSubscriptionStatuses.GracePeriod && artist.Subscription.CurrentPeriodEndsAt > now) ||
             (artist.Subscription.Status == ArtistSubscriptionStatuses.Active && artist.Subscription.CurrentPeriodEndsAt > now)));
}
