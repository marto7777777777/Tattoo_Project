using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.SubscriptionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public class ArtistSubscriptionService(TattooDbContext db, IConfiguration config) : IArtistSubscriptionService
{
    public async Task<bool> HasAccessAsync(string userId) => await db.ArtistSubscriptions.AnyAsync(x => x.TattooArtist.UserId == userId && (x.Status == ArtistSubscriptionStatuses.Trialing || x.Status == ArtistSubscriptionStatuses.Active));

    public async Task<ResultService<SubscriptionStatusDto>> GetStatusAsync(string userId)
    {
        var artist = await db.TattooArtists.AsNoTracking().Include(x=>x.Subscription).FirstOrDefaultAsync(x=>x.UserId==userId);
        if(artist==null) return ResultService<SubscriptionStatusDto>.Ok(new(){Status="not_applicable"});
        var s=artist.Subscription;
        return ResultService<SubscriptionStatusDto>.Ok(new(){Status=s?.Status??ArtistSubscriptionStatuses.Pending,HasAccess=s!=null&&ArtistSubscriptionStatuses.GrantsAccess(s.Status),RequiresCheckout=s==null||s.Status is ArtistSubscriptionStatuses.Pending or ArtistSubscriptionStatuses.Ended,CancelAtPeriodEnd=s?.CancelAtPeriodEnd??false,TrialEndsAt=s?.TrialEndsAt,CurrentPeriodEndsAt=s?.CurrentPeriodEndsAt});
    }

    public async Task<ResultService<StripeRedirectDto>> CreateCheckoutAsync(string userId)
    {
        var secret=config["Stripe:SecretKey"];var price=config["Stripe:ArtistMonthlyPriceId"];
        if(string.IsNullOrWhiteSpace(secret)||string.IsNullOrWhiteSpace(price))return ResultService<StripeRedirectDto>.Fail("Stripe Sandbox is not configured.");
        StripeConfiguration.ApiKey=secret;
        var artist=await db.TattooArtists.Include(x=>x.Subscription).Include(x=>x.User).FirstOrDefaultAsync(x=>x.UserId==userId);
        if(artist==null)return ResultService<StripeRedirectDto>.Fail("Artist profile was not found.");
        var s=artist.Subscription??new ArtistSubscription{TattooArtistId=artist.Id,Status=ArtistSubscriptionStatuses.Pending,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};
        if(artist.Subscription==null)db.ArtistSubscriptions.Add(s);
        if(ArtistSubscriptionStatuses.GrantsAccess(s.Status))return ResultService<StripeRedirectDto>.Fail("The artist subscription is already active.");
        if(string.IsNullOrWhiteSpace(s.StripeCustomerId))
        {
            var customer=await new CustomerService().CreateAsync(new CustomerCreateOptions{Email=artist.User.Email,Name=$"{artist.User.FirstName} {artist.User.LastName}",Metadata=new Dictionary<string,string>{{"artistId",artist.Id.ToString()}}});
            s.StripeCustomerId=customer.Id;
        }
        var frontend=(config["FrontendUrl"]??"http://localhost:5173").TrimEnd('/');
        var options=new SessionCreateOptions{Mode="subscription",Customer=s.StripeCustomerId,SuccessUrl=$"{frontend}/subscription?checkout=returned",CancelUrl=$"{frontend}/subscription?checkout=cancelled",AutomaticTax=new SessionAutomaticTaxOptions{Enabled=true},TaxIdCollection=new SessionTaxIdCollectionOptions{Enabled=true},BillingAddressCollection="required",PaymentMethodCollection="always",LineItems=[new SessionLineItemOptions{Price=price,Quantity=1}],SubscriptionData=new SessionSubscriptionDataOptions{TrialEnd=DateTime.UtcNow.AddMonths(3),Metadata=new Dictionary<string,string>{{"artistId",artist.Id.ToString()}}},Metadata=new Dictionary<string,string>{{"purpose","artist_subscription"},{"artistId",artist.Id.ToString()}}};
        var session=await new SessionService().CreateAsync(options);
        s.StripeCheckoutSessionId=session.Id;s.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();
        return ResultService<StripeRedirectDto>.Ok(new(){Url=session.Url});
    }

    public async Task<ResultService<StripeRedirectDto>> CreatePortalAsync(string userId)
    {
        var secret=config["Stripe:SecretKey"];if(string.IsNullOrWhiteSpace(secret))return ResultService<StripeRedirectDto>.Fail("Stripe Sandbox is not configured.");StripeConfiguration.ApiKey=secret;
        var s=await db.ArtistSubscriptions.FirstOrDefaultAsync(x=>x.TattooArtist.UserId==userId);
        if(string.IsNullOrWhiteSpace(s?.StripeCustomerId))return ResultService<StripeRedirectDto>.Fail("Stripe customer was not found.");
        var frontend=(config["FrontendUrl"]??"http://localhost:5173").TrimEnd('/');
        var options=new Stripe.BillingPortal.SessionCreateOptions{Customer=s.StripeCustomerId,ReturnUrl=$"{frontend}/subscription"};
        var portalConfig=config["Stripe:CustomerPortalConfigurationId"];if(!string.IsNullOrWhiteSpace(portalConfig))options.Configuration=portalConfig;
        var session=await new Stripe.BillingPortal.SessionService().CreateAsync(options);return ResultService<StripeRedirectDto>.Ok(new(){Url=session.Url});
    }

    public async Task<ResultService> ProcessVerifiedEventAsync(Event e)
    {
        switch(e.Type)
        {
            case "checkout.session.completed": await HandleCheckout((Stripe.Checkout.Session)e.Data.Object); break;
            case "customer.subscription.created":
            case "customer.subscription.updated": await UpsertSubscription((Subscription)e.Data.Object,false); break;
            case "customer.subscription.deleted": await UpsertSubscription((Subscription)e.Data.Object,true); break;
            case "invoice.paid": await HandleInvoicePaid((Invoice)e.Data.Object); break;
            case "invoice.payment_failed": await HandleInvoiceFailed((Invoice)e.Data.Object); break;
        }
        return ResultService.Ok();
    }

    private async Task HandleCheckout(Stripe.Checkout.Session session)
    {
        if(session.Metadata?.GetValueOrDefault("purpose")!="artist_subscription")return;
        var s=await db.ArtistSubscriptions.FirstOrDefaultAsync(x=>x.StripeCheckoutSessionId==session.Id);
        if(s==null)return;s.StripeCustomerId=session.CustomerId??s.StripeCustomerId;s.StripeSubscriptionId=session.SubscriptionId??s.StripeSubscriptionId;s.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();
    }

    private async Task UpsertSubscription(Subscription stripe,bool deleted)
    {
        var s=await db.ArtistSubscriptions.FirstOrDefaultAsync(x=>x.StripeSubscriptionId==stripe.Id||x.StripeCustomerId==stripe.CustomerId);
        if(s==null&&stripe.Metadata?.TryGetValue("artistId",out var raw)==true&&int.TryParse(raw,out var artistId)){s=new(){TattooArtistId=artistId,CreatedAt=DateTime.UtcNow};db.ArtistSubscriptions.Add(s);}if(s==null)return;
        var previousStatus=s.Status;var wasCancelAtPeriodEnd=s.CancelAtPeriodEnd;
        s.StripeSubscriptionId=stripe.Id;s.StripeCustomerId=stripe.CustomerId;s.Status=deleted?ArtistSubscriptionStatuses.Ended:MapStatus(stripe.Status);s.CancelAtPeriodEnd=stripe.CancelAtPeriodEnd;s.TrialEndsAt=stripe.TrialEnd;s.CurrentPeriodEndsAt=stripe.Items?.Data?.Select(x=>(DateTime?)x.CurrentPeriodEnd).Max();s.EndedAt=deleted?DateTime.UtcNow:null;s.UpdatedAt=DateTime.UtcNow;
        if(previousStatus!=ArtistSubscriptionStatuses.Trialing&&s.Status==ArtistSubscriptionStatuses.Trialing)db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=s.TattooArtistId,Name="subscription_trial_started",CreatedAt=DateTime.UtcNow});
        if(deleted&&previousStatus!=ArtistSubscriptionStatuses.Ended){db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=s.TattooArtistId,Name="subscription_cancelled",CreatedAt=DateTime.UtcNow});db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=s.TattooArtistId,Name="subscription_ended",CreatedAt=DateTime.UtcNow});}else if(!wasCancelAtPeriodEnd&&stripe.CancelAtPeriodEnd)db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=s.TattooArtistId,Name="subscription_cancelled",CreatedAt=DateTime.UtcNow});
        await db.SaveChangesAsync();
    }

    private async Task HandleInvoicePaid(Invoice invoice)
    {
        if(invoice.AmountPaid<=0||invoice.Parent?.SubscriptionDetails?.SubscriptionId is not string subscriptionId)return;
        var s=await db.ArtistSubscriptions.FirstOrDefaultAsync(x=>x.StripeSubscriptionId==subscriptionId);if(s==null)return;
        s.SuccessfulPaidInvoiceCount++;
        var milestone=s.SuccessfulPaidInvoiceCount==1?AnalyticsMilestones.FirstSubscriptionPayment:s.SuccessfulPaidInvoiceCount==2?AnalyticsMilestones.SecondSubscriptionPayment:null;
        if(milestone!=null&&!await db.ArtistAnalyticsMilestones.AnyAsync(x=>x.TattooArtistId==s.TattooArtistId&&x.Name==milestone)){db.ArtistAnalyticsMilestones.Add(new(){TattooArtistId=s.TattooArtistId,Name=milestone,OccurredAt=DateTime.UtcNow});db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=s.TattooArtistId,Name=milestone,CreatedAt=DateTime.UtcNow});}
        s.Status=ArtistSubscriptionStatuses.Active;s.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();
    }

    private async Task HandleInvoiceFailed(Invoice invoice)
    {
        if(invoice.Parent?.SubscriptionDetails?.SubscriptionId is not string subscriptionId)return;var s=await db.ArtistSubscriptions.FirstOrDefaultAsync(x=>x.StripeSubscriptionId==subscriptionId);if(s==null)return;s.Status=ArtistSubscriptionStatuses.PastDue;s.UpdatedAt=DateTime.UtcNow;db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=s.TattooArtistId,Name="subscription_payment_failed",CreatedAt=DateTime.UtcNow});await db.SaveChangesAsync();
    }

    private static string MapStatus(string status)=>status switch{"trialing"=>ArtistSubscriptionStatuses.Trialing,"active"=>ArtistSubscriptionStatuses.Active,"past_due" or "unpaid"=>ArtistSubscriptionStatuses.PastDue,"canceled"=>ArtistSubscriptionStatuses.Ended,"incomplete" or "incomplete_expired"=>ArtistSubscriptionStatuses.Incomplete,_=>ArtistSubscriptionStatuses.Pending};
}
