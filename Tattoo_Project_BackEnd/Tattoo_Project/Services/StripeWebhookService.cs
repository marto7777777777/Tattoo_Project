using Microsoft.EntityFrameworkCore;
using Stripe;
using Tattoo_Project.Data;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;
namespace Tattoo_Project.Services;
public class StripeWebhookService(TattooDbContext db,IConfiguration config,IArtistSubscriptionService subscriptions,IAiTattooService ai):IStripeWebhookService
{
 public async Task<ResultService> ProcessAsync(string payload,string signature)
 {
  var secret=config["Stripe:WebhookSecret"];if(string.IsNullOrWhiteSpace(secret))return ResultService.Fail("Stripe webhook is not configured.");
  Event e;try{e=EventUtility.ConstructEvent(payload,signature,secret,300,false);}catch(StripeException){return ResultService.Fail("Invalid Stripe signature.");}
  var record=await db.StripeWebhookEvents.FirstOrDefaultAsync(x=>x.StripeEventId==e.Id);
  if(record?.Status=="processed")return ResultService.Ok();
  if(record?.Status=="processing"&&record.ReceivedAt>DateTime.UtcNow.AddMinutes(-10))return ResultService.Ok();
  if(record==null){record=new StripeWebhookEvent{StripeEventId=e.Id,EventType=e.Type,ReceivedAt=DateTime.UtcNow};db.StripeWebhookEvents.Add(record);try{await db.SaveChangesAsync();}catch(DbUpdateException){return ResultService.Ok();}}
  else{record.Status="processing";record.Error=null;record.ReceivedAt=DateTime.UtcNow;await db.SaveChangesAsync();}
  try
  {
   if(e.Type=="checkout.session.completed"&&e.Data.Object is Stripe.Checkout.Session session&&session.Metadata?.GetValueOrDefault("purpose")!="artist_subscription")await ai.ProcessVerifiedStripeEventAsync(e);
   else await subscriptions.ProcessVerifiedEventAsync(e);
   record.Status="processed";record.ProcessedAt=DateTime.UtcNow;await db.SaveChangesAsync();return ResultService.Ok();
  }
  catch(Exception ex){record.Status="failed";record.Error=ex.Message[..Math.Min(1000,ex.Message.Length)];await db.SaveChangesAsync();return ResultService.Fail("Stripe webhook processing failed.");}
 }
}
