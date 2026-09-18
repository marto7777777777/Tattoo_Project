using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.SubscriptionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public class ArtistSubscriptionService(
    TattooDbContext db,
    IConfiguration config,
    IPurchasePayloadProtector payloadProtector,
    TimeProvider timeProvider) : IArtistSubscriptionService
{
    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<bool> HasAccessAsync(string userId)
    {
        var s=await db.ArtistSubscriptions.AsNoTracking().FirstOrDefaultAsync(x=>x.TattooArtist.UserId==userId);
        return s!=null&&SubscriptionEntitlementRules.HasAccess(s,UtcNow);
    }

    public async Task<ResultService<SubscriptionStatusDto>> GetStatusAsync(string userId)
    {
        var artist = await db.TattooArtists.AsNoTracking().Include(x=>x.Subscription).ThenInclude(x=>x.ProviderSubscriptions).FirstOrDefaultAsync(x=>x.UserId==userId);
        if(artist==null) return ResultService<SubscriptionStatusDto>.Ok(new(){Status="not_applicable"});
        var s=artist.Subscription;
        var hasAccess=s!=null&&SubscriptionEntitlementRules.HasAccess(s,UtcNow);return ResultService<SubscriptionStatusDto>.Ok(new(){Status=s?.Status??ArtistSubscriptionStatuses.Pending,HasAccess=hasAccess,RequiresCheckout=!hasAccess,CancelAtPeriodEnd=s?.CancelAtPeriodEnd??false,TrialEndsAt=s?.TrialEndsAt,CurrentPeriodEndsAt=s?.CurrentPeriodEndsAt,ActiveProvider=s?.ActiveProvider,HasUsedTrial=s?.HasUsedTrial??false,LastVerifiedAt=s?.LastVerifiedAt,IsSandboxEntitlement=s?.ActiveProvider!=null&&s.ProviderSubscriptions.Any(p=>p.Provider==s.ActiveProvider&&p.IsSandbox)});
    }

    public async Task<ResultService<StripeRedirectDto>> CreateCheckoutAsync(string userId)
    {
        var secret=config["Stripe:SecretKey"];var price=config["Stripe:ArtistMonthlyPriceId"];
        if(string.IsNullOrWhiteSpace(secret)||string.IsNullOrWhiteSpace(price))return ResultService<StripeRedirectDto>.Fail("Stripe Sandbox is not configured.");
        StripeConfiguration.ApiKey=secret;
        await using var transaction=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var artist=await db.TattooArtists.Include(x=>x.Subscription).Include(x=>x.User).FirstOrDefaultAsync(x=>x.UserId==userId);
        if(artist==null)return ResultService<StripeRedirectDto>.Fail("Artist profile was not found.");
        var s=artist.Subscription??new ArtistSubscription{TattooArtistId=artist.Id,Status=ArtistSubscriptionStatuses.Pending,CreatedAt=UtcNow,UpdatedAt=UtcNow};
        if(artist.Subscription==null)db.ArtistSubscriptions.Add(s);
        var providerRows=await db.ProviderSubscriptions.Where(x=>x.ArtistSubscriptionId==s.Id).ToListAsync();
        var now=UtcNow;
        if(SubscriptionEntitlementRules.HasAccess(s,now)||providerRows.Any(x=>SubscriptionEntitlementRules.ProviderHasAccess(x,now)))return ResultService<StripeRedirectDto>.Fail("An artist subscription is already active.");
        if(s.PendingProviderExpiresAt>now&&s.PendingProvider!=SubscriptionProviders.Stripe)return ResultService<StripeRedirectDto>.Fail("A subscription purchase is already in progress with another billing provider.");
        if(!string.IsNullOrWhiteSpace(s.StripeSubscriptionId))return ResultService<StripeRedirectDto>.Fail("The existing Stripe subscription is awaiting webhook confirmation.");
        if(!string.IsNullOrWhiteSpace(s.StripeCheckoutSessionId)){try{var pendingSession=await new SessionService().GetAsync(s.StripeCheckoutSessionId);if(pendingSession.Status=="open"&&!string.IsNullOrWhiteSpace(pendingSession.Url)){s.PendingProvider=SubscriptionProviders.Stripe;s.PendingProviderExpiresAt=now.AddMinutes(30);await db.SaveChangesAsync();await transaction.CommitAsync();return ResultService<StripeRedirectDto>.Ok(new(){Url=pendingSession.Url});}}catch(StripeException){/* create a fresh recoverable Checkout session */}s.StripeCheckoutAttemptToken=Guid.NewGuid();}
        if(s.StripeCheckoutAttemptToken==null||s.PendingProviderExpiresAt<=now)s.StripeCheckoutAttemptToken=Guid.NewGuid();s.PendingProvider=SubscriptionProviders.Stripe;s.PendingProviderExpiresAt=now.AddMinutes(30);await db.SaveChangesAsync();await transaction.CommitAsync();var attempt=s.StripeCheckoutAttemptToken.Value.ToString("N");
        if(string.IsNullOrWhiteSpace(s.StripeCustomerId))
        {
            var customer=await new CustomerService().CreateAsync(new CustomerCreateOptions{Email=artist.User.Email,Name=$"{artist.User.FirstName} {artist.User.LastName}",Metadata=new Dictionary<string,string>{{"artistId",artist.Id.ToString()}}},new RequestOptions{IdempotencyKey=$"artist-customer-{artist.Id}"});
            s.StripeCustomerId=customer.Id;
        }
        var frontend=(config["FrontendUrl"]??"http://localhost:5173").TrimEnd('/');
        var subscriptionData=new SessionSubscriptionDataOptions{Metadata=new Dictionary<string,string>{{"artistId",artist.Id.ToString()}}};if(!s.HasUsedTrial)subscriptionData.TrialEnd=UtcNow.AddMonths(3);
        var options=new SessionCreateOptions{Mode="subscription",Customer=s.StripeCustomerId,SuccessUrl=$"{frontend}/subscription?checkout=returned",CancelUrl=$"{frontend}/subscription?checkout=cancelled",AutomaticTax=new SessionAutomaticTaxOptions{Enabled=true},TaxIdCollection=new SessionTaxIdCollectionOptions{Enabled=true},BillingAddressCollection="required",PaymentMethodCollection="always",LineItems=[new SessionLineItemOptions{Price=price,Quantity=1}],SubscriptionData=subscriptionData,Metadata=new Dictionary<string,string>{{"purpose","artist_subscription"},{"artistId",artist.Id.ToString()}}};
        var session=await new SessionService().CreateAsync(options,new RequestOptions{IdempotencyKey=$"artist-checkout-{artist.Id}-{attempt}"});
        s.StripeCheckoutSessionId=session.Id;s.UpdatedAt=UtcNow;await db.SaveChangesAsync();
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
            case "customer.subscription.updated": await UpsertSubscription((Subscription)e.Data.Object,false,null,e.Created,e.Id); break;
            case "customer.subscription.deleted": await UpsertSubscription((Subscription)e.Data.Object,true,null,e.Created,e.Id); break;
            case "invoice.paid": await HandleInvoicePaid((Invoice)e.Data.Object,e.Created,e.Id); break;
            case "invoice.payment_failed": await HandleInvoiceFailed((Invoice)e.Data.Object,e.Created,e.Id); break;
        }
        return ResultService.Ok();
    }

    private async Task HandleCheckout(Stripe.Checkout.Session session)
    {
        if(session.Metadata?.GetValueOrDefault("purpose")!="artist_subscription")return;
        var s=await db.ArtistSubscriptions.FirstOrDefaultAsync(x=>x.StripeCheckoutSessionId==session.Id);
        if(s==null)return;s.StripeCustomerId=session.CustomerId??s.StripeCustomerId;s.StripeSubscriptionId=session.SubscriptionId??s.StripeSubscriptionId;s.UpdatedAt=UtcNow;await db.SaveChangesAsync();
    }

    private async Task UpsertSubscription(Subscription stripe,bool deleted,string? verifiedTransactionId=null,DateTime? providerEventAt=null,string? providerEventId=null)
    {
        var s=await db.ArtistSubscriptions.Include(x=>x.TattooArtist).FirstOrDefaultAsync(x=>x.StripeSubscriptionId==stripe.Id||x.StripeCustomerId==stripe.CustomerId);
        if(s==null&&stripe.Metadata?.TryGetValue("artistId",out var raw)==true&&int.TryParse(raw,out var artistId))
        {
            var artist=await db.TattooArtists.Include(x=>x.User).FirstOrDefaultAsync(x=>x.Id==artistId);if(artist==null)return;
            s=new(){TattooArtistId=artistId,TattooArtist=artist,CreatedAt=UtcNow,UpdatedAt=UtcNow,Status=ArtistSubscriptionStatuses.Pending};db.ArtistSubscriptions.Add(s);
        }
        if(s==null)return;
        s.StripeSubscriptionId=stripe.Id;s.StripeCustomerId=stripe.CustomerId;s.UpdatedAt=UtcNow;await db.SaveChangesAsync();
        var configuredPrice=config["Stripe:ArtistMonthlyPriceId"];var actualPrice=stripe.Items?.Data?.FirstOrDefault()?.Price?.Id;var priceMatches=!string.IsNullOrWhiteSpace(configuredPrice)&&actualPrice==configuredPrice;var mapped=!priceMatches?ArtistSubscriptionStatuses.Revoked:deleted?ArtistSubscriptionStatuses.Expired:MapStatus(stripe.Status);var periodEnd=stripe.Items?.Data?.Select(x=>(DateTime?)x.CurrentPeriodEnd).Max();
        var applied=await ApplyVerifiedProviderAsync(s.TattooArtist?.UserId,new(){Provider=SubscriptionProviders.Stripe,ExternalSubscriptionId=stripe.Id,ExternalTransactionId=verifiedTransactionId,ProductId=actualPrice??"unverified_stripe_price",Status=mapped,TrialEndsAt=stripe.TrialEnd,CurrentPeriodEndsAt=periodEnd,CancelAtPeriodEnd=stripe.CancelAtPeriodEnd,IsSandbox=!stripe.Livemode,IsTrial=priceMatches&&mapped==ArtistSubscriptionStatuses.Trialing,ProviderEventAt=providerEventAt,ProviderEventId=providerEventId});
        if(!applied.Success)throw new InvalidOperationException(applied.ErrorMessage);
    }

    private async Task HandleInvoicePaid(Invoice invoice,DateTime providerEventAt,string providerEventId)
    {
        if(invoice.AmountPaid<=0||invoice.Parent?.SubscriptionDetails?.SubscriptionId is not string subscriptionId)return;
        var verifiedSubscription=await new SubscriptionService().GetAsync(subscriptionId);
        await UpsertSubscription(verifiedSubscription,false,invoice.Id,providerEventAt,providerEventId);
    }

    private async Task HandleInvoiceFailed(Invoice invoice,DateTime providerEventAt,string providerEventId)
    {
        if(invoice.Parent?.SubscriptionDetails?.SubscriptionId is not string subscriptionId)return;var verifiedSubscription=await new SubscriptionService().GetAsync(subscriptionId);await UpsertSubscription(verifiedSubscription,false,null,providerEventAt,providerEventId);var s=await db.ArtistSubscriptions.FirstOrDefaultAsync(x=>x.StripeSubscriptionId==subscriptionId);if(s==null)return;s.UpdatedAt=UtcNow;db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=s.TattooArtistId,Name="subscription_payment_failed",CreatedAt=UtcNow});await db.SaveChangesAsync();
    }

    public async Task<ResultService<MobileBillingContextDto>> GetMobileContextAsync(string userId,string provider)
    {
        if(provider is not (SubscriptionProviders.GooglePlay or SubscriptionProviders.Apple))return ResultService<MobileBillingContextDto>.Fail("Unsupported mobile billing provider.");
        await using var transaction=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);var s=await db.ArtistSubscriptions.FirstOrDefaultAsync(x=>x.TattooArtist.UserId==userId);if(s==null)return ResultService<MobileBillingContextDto>.Fail("Artist subscription was not found.");var now=UtcNow;if(SubscriptionEntitlementRules.HasAccess(s,now)&&s.ActiveProvider!=provider)return ResultService<MobileBillingContextDto>.Fail("An artist subscription is already active with another billing provider.");if(s.PendingProviderExpiresAt>now&&s.PendingProvider!=provider)return ResultService<MobileBillingContextDto>.Fail("A subscription purchase is already in progress with another billing provider.");s.PendingProvider=provider;s.PendingProviderExpiresAt=now.AddMinutes(30);
        if(provider==SubscriptionProviders.GooglePlay&&string.IsNullOrWhiteSpace(s.GoogleObfuscatedAccountId)){var secret=config["Billing:AccountObfuscationSecret"];if(string.IsNullOrWhiteSpace(secret))return ResultService<MobileBillingContextDto>.Fail("Mobile billing is not configured.");using var h=new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));s.GoogleObfuscatedAccountId=Convert.ToHexString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(userId))).ToLowerInvariant();}
        if(provider==SubscriptionProviders.Apple&&s.AppleAppAccountToken==null)s.AppleAppAccountToken=Guid.NewGuid();await db.SaveChangesAsync();await transaction.CommitAsync();
        return ResultService<MobileBillingContextDto>.Ok(new(){Provider=provider,ArtistProductId=config[provider==SubscriptionProviders.GooglePlay?"GooglePlay:ArtistSubscriptionProductId":"Apple:ArtistSubscriptionProductId"]??"",AiProjectPassProductId=config[provider==SubscriptionProviders.GooglePlay?"GooglePlay:AiProjectPassProductId":"Apple:AiProjectPassProductId"]??"",BasePlanId=provider==SubscriptionProviders.GooglePlay?config["GooglePlay:ArtistBasePlanId"]:null,TrialOfferId=provider==SubscriptionProviders.GooglePlay?config["GooglePlay:ArtistTrialOfferId"]:null,TrialEligible=!s.HasUsedTrial,ObfuscatedAccountId=provider==SubscriptionProviders.GooglePlay?s.GoogleObfuscatedAccountId:null,AppAccountToken=provider==SubscriptionProviders.Apple?s.AppleAppAccountToken:null});
    }
    public async Task<ResultService<MobileBillingContextDto>> GetAiMobileContextAsync(string userId,string provider)
    {
        if(provider is not (SubscriptionProviders.GooglePlay or SubscriptionProviders.Apple))return ResultService<MobileBillingContextDto>.Fail("Unsupported mobile billing provider.");var user=await db.Users.FirstOrDefaultAsync(x=>x.Id==userId);if(user==null)return ResultService<MobileBillingContextDto>.Fail("User was not found.");
        if(provider==SubscriptionProviders.GooglePlay&&string.IsNullOrWhiteSpace(user.GoogleBillingObfuscatedAccountId)){var secret=config["Billing:AccountObfuscationSecret"];if(string.IsNullOrWhiteSpace(secret))return ResultService<MobileBillingContextDto>.Fail("Mobile billing is not configured.");using var h=new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));user.GoogleBillingObfuscatedAccountId=Convert.ToHexString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(userId))).ToLowerInvariant();}
        if(provider==SubscriptionProviders.Apple&&user.AppleBillingAppAccountToken==null)user.AppleBillingAppAccountToken=Guid.NewGuid();await db.SaveChangesAsync();return ResultService<MobileBillingContextDto>.Ok(new(){Provider=provider,AiProjectPassProductId=config[provider==SubscriptionProviders.GooglePlay?"GooglePlay:AiProjectPassProductId":"Apple:AiProjectPassProductId"]??"",ArtistProductId="",TrialEligible=false,ObfuscatedAccountId=provider==SubscriptionProviders.GooglePlay?user.GoogleBillingObfuscatedAccountId:null,AppAccountToken=provider==SubscriptionProviders.Apple?user.AppleBillingAppAccountToken:null});
    }

    public async Task<ResultService> ApplyVerifiedProviderAsync(string? userId,VerifiedProviderSubscription state)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var existing=await db.ProviderSubscriptions.Include(x=>x.ArtistSubscription).ThenInclude(x=>x.TattooArtist).FirstOrDefaultAsync(x=>x.Provider==state.Provider&&x.ExternalSubscriptionId==state.ExternalSubscriptionId);
        ArtistSubscription? aggregate=existing?.ArtistSubscription;
        if(userId!=null){var requested=await db.ArtistSubscriptions.Include(x=>x.TattooArtist).FirstOrDefaultAsync(x=>x.TattooArtist.UserId==userId);if(requested==null)return ResultService.Fail("Artist subscription was not found.");if(aggregate!=null&&aggregate.Id!=requested.Id)return ResultService.Fail("This store purchase is linked to another InkRoute account.");aggregate=requested;}
        if(aggregate==null)return ResultService.Fail("The provider subscription is not linked to an InkRoute account.");
        var verificationTime=UtcNow;if(userId!=null&&existing==null&&aggregate.PendingProviderExpiresAt>verificationTime&&aggregate.PendingProvider!=state.Provider)return ResultService.Fail("A subscription purchase is already in progress with another billing provider.");
        if(state.IsTrial&&aggregate.HasUsedTrial&&existing==null)return ResultService.Fail("The free trial has already been used by this InkRoute artist account.");
        var bindingHash=payloadProtector.Hash(state.Provider+":"+state.ExternalSubscriptionId);var binding=await db.StorePurchaseBindings.FirstOrDefaultAsync(x=>x.Provider==state.Provider&&x.BindingKeyHash==bindingHash);if(binding!=null&&binding.OriginalArtistSubscriptionId!=aggregate.Id)return ResultService.Fail("This store purchase is linked to another InkRoute account.");if(binding==null)db.StorePurchaseBindings.Add(new(){Provider=state.Provider,BindingKeyHash=bindingHash,OriginalArtistSubscriptionId=aggregate.Id,CreatedAt=UtcNow});
        var now=verificationTime;
        if(existing!=null && !ProviderEventOrdering.ShouldApply(existing.LastProviderEventAt,existing.LastProviderEventId,state.ProviderEventAt,state.ProviderEventId)) return ResultService.Ok();
        var allowSandbox=config.GetValue<bool>("Billing:AllowSandboxEntitlements");
        var effective=!BillingSandboxPolicy.CanGrant(state.IsSandbox,allowSandbox) ? ArtistSubscriptionStatuses.Expired : SubscriptionEntitlementRules.Normalize(state.Status,state.TrialEndsAt,state.CurrentPeriodEndsAt,state.CancelAtPeriodEnd,state.IsTrial,now);if(ArtistSubscriptionStatuses.GrantsAccess(effective)){var otherSubscriptions=await db.ProviderSubscriptions.Where(x=>x.ArtistSubscriptionId==aggregate.Id&&!(x.Provider==state.Provider&&x.ExternalSubscriptionId==state.ExternalSubscriptionId)).ToListAsync();if(otherSubscriptions.Any(x=>SubscriptionEntitlementRules.ProviderHasAccess(x,now)))return ResultService.Fail("Another billing subscription is already active.");}
        var previousAggregateStatus=aggregate.Status;var previousCancel=aggregate.CancelAtPeriodEnd;var previousTransaction=existing?.ExternalTransactionId;existing??=new(){ArtistSubscriptionId=aggregate.Id,Provider=state.Provider,ExternalSubscriptionId=state.ExternalSubscriptionId,CreatedAt=now,FirstVerifiedAt=now};if(existing.Id==0)db.ProviderSubscriptions.Add(existing);
        if(!string.IsNullOrWhiteSpace(state.ExternalTransactionId))existing.ExternalTransactionId=state.ExternalTransactionId;if(state.ProviderEventAt.HasValue)existing.LastProviderEventAt=state.ProviderEventAt;if(!string.IsNullOrWhiteSpace(state.ProviderEventId))existing.LastProviderEventId=state.ProviderEventId;existing.ProductId=state.ProductId;existing.Status=state.Status;existing.TrialStartsAt=state.TrialStartsAt;existing.TrialEndsAt=state.TrialEndsAt;existing.CurrentPeriodStartsAt=state.CurrentPeriodStartsAt;existing.CurrentPeriodEndsAt=state.CurrentPeriodEndsAt;existing.CancelAtPeriodEnd=state.CancelAtPeriodEnd;existing.IsSandbox=state.IsSandbox;existing.LastVerifiedAt=now;existing.UpdatedAt=now;if(!string.IsNullOrWhiteSpace(state.RawPurchasePayload)){existing.PurchaseTokenHash=payloadProtector.Hash(state.RawPurchasePayload);existing.EncryptedPurchasePayload=payloadProtector.Protect(state.RawPurchasePayload);}
        aggregate.Status=effective;aggregate.ActiveProvider=ArtistSubscriptionStatuses.GrantsAccess(effective)?state.Provider:null;aggregate.CancelAtPeriodEnd=state.CancelAtPeriodEnd;aggregate.TrialEndsAt=state.TrialEndsAt;aggregate.CurrentPeriodEndsAt=state.CurrentPeriodEndsAt;aggregate.LastVerifiedAt=now;aggregate.UpdatedAt=now;aggregate.PendingProvider=null;aggregate.PendingProviderExpiresAt=null;if(state.IsTrial)aggregate.HasUsedTrial=true;if(ArtistSubscriptionStatuses.GrantsAccess(effective)&&aggregate.FirstActivatedAt==null)aggregate.FirstActivatedAt=now;var endedNow=state.Status==ArtistSubscriptionStatuses.Revoked||!ArtistSubscriptionStatuses.GrantsAccess(effective)&&state.CurrentPeriodEndsAt<=now;if(endedNow)aggregate.EndedAt=now;
        if(previousAggregateStatus!=ArtistSubscriptionStatuses.Trialing&&effective==ArtistSubscriptionStatuses.Trialing)db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=aggregate.TattooArtistId,Name="subscription_trial_started",CreatedAt=now});
        if(!previousCancel&&state.CancelAtPeriodEnd)db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=aggregate.TattooArtistId,Name="subscription_cancelled",CreatedAt=now});
        if(ArtistSubscriptionStatuses.GrantsAccess(previousAggregateStatus)&&endedNow)db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=aggregate.TattooArtistId,Name="subscription_ended",CreatedAt=now});
        if(effective==ArtistSubscriptionStatuses.Active&&!state.IsTrial&&!string.IsNullOrWhiteSpace(state.ExternalTransactionId)&&state.ExternalTransactionId!=previousTransaction)
        {
            aggregate.SuccessfulPaidInvoiceCount++;
            var milestone=aggregate.SuccessfulPaidInvoiceCount==1?AnalyticsMilestones.FirstSubscriptionPayment:aggregate.SuccessfulPaidInvoiceCount==2?AnalyticsMilestones.SecondSubscriptionPayment:null;
            if(milestone!=null&&!await db.ArtistAnalyticsMilestones.AnyAsync(x=>x.TattooArtistId==aggregate.TattooArtistId&&x.Name==milestone)){db.ArtistAnalyticsMilestones.Add(new(){TattooArtistId=aggregate.TattooArtistId,Name=milestone,OccurredAt=now});db.AnalyticsOutboxEvents.Add(new(){TattooArtistId=aggregate.TattooArtistId,Name=milestone,CreatedAt=now});}
        }
        await db.SaveChangesAsync();await tx.CommitAsync();return ResultService.Ok();
    }

    private static string MapStatus(string status)=>status switch{"trialing"=>ArtistSubscriptionStatuses.Trialing,"active"=>ArtistSubscriptionStatuses.Active,"past_due" or "unpaid"=>ArtistSubscriptionStatuses.PastDue,"paused"=>ArtistSubscriptionStatuses.Paused,"canceled"=>ArtistSubscriptionStatuses.Expired,"incomplete" or "incomplete_expired"=>ArtistSubscriptionStatuses.Incomplete,_=>ArtistSubscriptionStatuses.Pending};
}
