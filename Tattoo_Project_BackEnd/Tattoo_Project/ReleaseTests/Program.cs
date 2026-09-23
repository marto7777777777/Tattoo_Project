using Tattoo_Project.Models;
using Tattoo_Project.Services;

var now = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
var failures = new List<string>();

void Equal(string name, object? expected, object? actual)
{
    if (!Equals(expected, actual)) failures.Add($"{name}: expected {expected ?? "<null>"}, got {actual ?? "<null>"}");
}

void True(string name, bool actual) => Equal(name, true, actual);
void False(string name, bool actual) => Equal(name, false, actual);

void Access(string name, string status, DateTime? trialEnd, DateTime? periodEnd, bool cancelled, bool expected)
{
    var subscription = new ArtistSubscription
    {
        Status = status,
        TrialEndsAt = trialEnd,
        CurrentPeriodEndsAt = periodEnd,
        CancelAtPeriodEnd = cancelled
    };
    Equal(name, expected, SubscriptionEntitlementRules.HasAccess(subscription, now));
}

// Artist subscription entitlement boundaries.
Access("active", ArtistSubscriptionStatuses.Active, null, now.AddMonths(1), false, true);
Access("active missing end", ArtistSubscriptionStatuses.Active, null, null, false, false);
Access("active cancelled before end", ArtistSubscriptionStatuses.Active, null, now.AddDays(1), true, true);
Access("active cancelled after end", ArtistSubscriptionStatuses.Active, null, now.AddSeconds(-1), true, false);
Access("trial before end", ArtistSubscriptionStatuses.Trialing, now.AddDays(1), now.AddDays(1), false, true);
Access("trial missing end", ArtistSubscriptionStatuses.Trialing, null, null, false, false);
Access("trial after end", ArtistSubscriptionStatuses.Trialing, now.AddSeconds(-1), now.AddSeconds(-1), false, false);
Access("grace before end", ArtistSubscriptionStatuses.GracePeriod, null, now.AddDays(1), false, true);
Access("grace after end", ArtistSubscriptionStatuses.GracePeriod, null, now.AddSeconds(-1), false, false);
Access("on hold", ArtistSubscriptionStatuses.OnHold, null, now.AddDays(30), true, false);
Access("paused", ArtistSubscriptionStatuses.Paused, null, now.AddDays(30), true, false);
Access("past due", ArtistSubscriptionStatuses.PastDue, null, now.AddDays(30), true, false);
Access("revoked", ArtistSubscriptionStatuses.Revoked, null, now.AddDays(30), true, false);
Access("expired", ArtistSubscriptionStatuses.Expired, null, now.AddDays(30), false, false);

Equal("cancelled paid period", ArtistSubscriptionStatuses.Active,
    SubscriptionEntitlementRules.Normalize(ArtistSubscriptionStatuses.Cancelled, null, now.AddDays(1), true, false, now));
Equal("cancelled trial period", ArtistSubscriptionStatuses.Trialing,
    SubscriptionEntitlementRules.Normalize(ArtistSubscriptionStatuses.Cancelled, now.AddDays(1), now.AddDays(1), true, true, now));
Equal("expired cancellation", ArtistSubscriptionStatuses.Expired,
    SubscriptionEntitlementRules.Normalize(ArtistSubscriptionStatuses.Cancelled, null, now.AddSeconds(-1), true, false, now));
Equal("active with missing period normalizes expired", ArtistSubscriptionStatuses.Expired,
    SubscriptionEntitlementRules.Normalize(ArtistSubscriptionStatuses.Active, null, null, false, false, now));
Equal("on hold is never upgraded", ArtistSubscriptionStatuses.OnHold,
    SubscriptionEntitlementRules.Normalize(ArtistSubscriptionStatuses.OnHold, null, now.AddDays(30), true, false, now));
Equal("paused is never upgraded", ArtistSubscriptionStatuses.Paused,
    SubscriptionEntitlementRules.Normalize(ArtistSubscriptionStatuses.Paused, null, now.AddDays(30), true, false, now));
Equal("past due is never upgraded", ArtistSubscriptionStatuses.PastDue,
    SubscriptionEntitlementRules.Normalize(ArtistSubscriptionStatuses.PastDue, null, now.AddDays(30), true, false, now));

// Central tattoo-request state machine.
True("submitted -> under review", TattooRequestStateMachine.CanTransition(RequestStatus.Submitted, RequestStatus.UnderReview));
True("submitted -> approved", TattooRequestStateMachine.CanTransition(RequestStatus.Submitted, RequestStatus.Approved));
True("submitted -> rejected", TattooRequestStateMachine.CanTransition(RequestStatus.Submitted, RequestStatus.Rejected));
False("approved -> rejected forbidden", TattooRequestStateMachine.CanTransition(RequestStatus.Approved, RequestStatus.Rejected));
True("approved -> cancelled", TattooRequestStateMachine.CanTransition(RequestStatus.Approved, RequestStatus.Cancelled));
True("approved -> tattoo booked", TattooRequestStateMachine.CanTransition(RequestStatus.Approved, RequestStatus.TattooBooked));
True("waiting consultation -> completed consultation", TattooRequestStateMachine.CanTransition(RequestStatus.WaitingForConsultation, RequestStatus.ConsultationCompleted));
True("completed -> in progress for continue tattoo", TattooRequestStateMachine.CanTransition(RequestStatus.Completed, RequestStatus.InProgress));
False("cancelled terminal", TattooRequestStateMachine.CanTransition(RequestStatus.Cancelled, RequestStatus.InProgress));
False("rejected terminal", TattooRequestStateMachine.CanTransition(RequestStatus.Rejected, RequestStatus.Approved));

// Timezone conversion and DST validation for Europe/Sofia.
var winter = TimeZoneSupport.ToUtc(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified), "Europe/Sofia");
True("Sofia winter conversion succeeds", winter.Success);
if (winter.Success) Equal("Sofia winter UTC", new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), winter.Data);
var summer = TimeZoneSupport.ToUtc(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Unspecified), "Europe/Sofia");
True("Sofia summer conversion succeeds", summer.Success);
if (summer.Success) Equal("Sofia summer UTC", new DateTime(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc), summer.Data);
var invalidDst = TimeZoneSupport.ToUtc(new DateTime(2026, 3, 29, 3, 30, 0, DateTimeKind.Unspecified), "Europe/Sofia");
False("Sofia spring-forward invalid local time rejected", invalidDst.Success);
var ambiguousDst = TimeZoneSupport.ToUtc(new DateTime(2026, 10, 25, 3, 30, 0, DateTimeKind.Unspecified), "Europe/Sofia");
False("Sofia fall-back ambiguous local time rejected", ambiguousDst.Success);
var invalidZone = TimeZoneSupport.Get("Not/A-TimeZone");
False("invalid IANA timezone rejected", invalidZone.Success);

// The web AI pass is a one-time purchase. Never accept a recurring Price,
// even when its amount and currency happen to match the configured pass.
Equal("AI Stripe Checkout uses payment mode", "payment", AiStripePriceRules.CheckoutMode);
True("AI Stripe one-time price accepted",
    AiStripePriceRules.IsValidOneTimePrice(true, false, 1249, "eur", "exclusive", 1249, "eur"));
False("AI Stripe recurring price rejected",
    AiStripePriceRules.IsValidOneTimePrice(true, true, 1249, "eur", "exclusive", 1249, "eur"));
False("AI Stripe inactive price rejected",
    AiStripePriceRules.IsValidOneTimePrice(false, false, 1249, "eur", "exclusive", 1249, "eur"));
False("AI Stripe wrong amount rejected",
    AiStripePriceRules.IsValidOneTimePrice(true, false, 1499, "eur", "exclusive", 1249, "eur"));
False("AI Stripe wrong currency rejected",
    AiStripePriceRules.IsValidOneTimePrice(true, false, 1249, "usd", "exclusive", 1249, "eur"));
False("AI Stripe inclusive tax price rejected",
    AiStripePriceRules.IsValidOneTimePrice(true, false, 1249, "eur", "inclusive", 1249, "eur"));

// Half-open interval rule used by booking conflict queries.
static bool Overlaps(DateTime start, DateTime end, DateTime existingStart, DateTime existingEnd) =>
    start < existingEnd && end > existingStart;
var d = new DateTime(2026, 9, 20, 9, 0, 0, DateTimeKind.Utc);
False("adjacent intervals do not overlap", Overlaps(d, d.AddMinutes(30), d.AddMinutes(30), d.AddMinutes(90)));
True("contained interval overlaps", Overlaps(d.AddMinutes(15), d.AddMinutes(30), d, d.AddMinutes(60)));
True("crossing interval overlaps", Overlaps(d.AddMinutes(45), d.AddMinutes(75), d, d.AddMinutes(60)));

if (failures.Count > 0)
{
    Console.Error.WriteLine("Release tests failed:");
    foreach (var failure in failures) Console.Error.WriteLine("- " + failure);
    return 1;
}

Console.WriteLine("All backend hardening release tests passed.");
return 0;
