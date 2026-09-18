using System.Net.Http.Headers;using System.Text;using System.Text.Json;using Google.Apis.Auth;using Google.Apis.Auth.OAuth2;using Microsoft.EntityFrameworkCore;using Tattoo_Project.Data;using Tattoo_Project.DTOs.SubscriptionDTOs;using Tattoo_Project.Models;using Tattoo_Project.Services.Interfaces;using Tattoo_Project.Services.Results;
namespace Tattoo_Project.Services;
public class MobileBillingService(TattooDbContext db,IConfiguration config,IHttpClientFactory clients,IArtistSubscriptionService subscriptions,IPurchasePayloadProtector protector,IProviderWebhookEventService events,AppleJwsVerifier appleVerifier,AppleServerApiClient appleApi,TimeProvider timeProvider):IMobileBillingService
{
 private DateTime UtcNow=>timeProvider.GetUtcNow().UtcDateTime;
 public async Task<ResultService<MobilePurchaseVerificationResultDto>> VerifyAsync(string userId,MobilePurchaseVerificationDto dto)
 {
  if(string.IsNullOrWhiteSpace(userId)||dto==null||string.IsNullOrWhiteSpace(dto.Provider)||string.IsNullOrWhiteSpace(dto.ProductId)||string.IsNullOrWhiteSpace(dto.PurchasePayload)||dto.ProductId.Length>200||dto.PurchasePayload.Length>1_048_576)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Invalid mobile purchase payload.");
  if(dto.ProductKind is not ("artist_subscription" or "ai_project_pass"))return ResultService<MobilePurchaseVerificationResultDto>.Fail("Unsupported product kind.");
  if(dto.ProductKind=="ai_project_pass")
  {
   if(dto.AiProjectId==null)return ResultService<MobilePurchaseVerificationResultDto>.Fail("AI project is required.");
   var touch=await TouchAiProjectForPurchaseVerificationAsync(userId,dto.AiProjectId.Value);
   if(!touch.Success)return ResultService<MobilePurchaseVerificationResultDto>.Fail(touch.ErrorMessage!);
  }
  if(dto.Provider==SubscriptionProviders.GooglePlay)return await VerifyGoogleAsync(userId,dto);
  if(dto.Provider==SubscriptionProviders.Apple)return await VerifyAppleAsync(userId,dto);
  return ResultService<MobilePurchaseVerificationResultDto>.Fail("Unsupported billing provider.");
 }

 private async Task<ResultService> TouchAiProjectForPurchaseVerificationAsync(string userId,int projectId)
 {
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
  await DatabaseApplicationLock.AcquireAsync(db,$"InkRoute:AiPass:{projectId}","ai_pass_concurrency_conflict","The AI Project Pass is being updated by another request. Retry verification.");
  db.ChangeTracker.Clear();
  var project=await db.AiTattooProjects.FirstOrDefaultAsync(x=>x.Id==projectId&&x.UserId==userId);
  if(project==null||project.IsFreeProject){await tx.RollbackAsync();return ResultService.Fail("A paid AI draft owned by the user is required.");}
  project.UpdatedAt=UtcNow;
  await db.SaveChangesAsync();await tx.CommitAsync();return ResultService.Ok();
 }

 private async Task<ResultService<MobilePurchaseVerificationResultDto>> VerifyGoogleAsync(string userId,MobilePurchaseVerificationDto dto)
 {
  var package=config["GooglePlay:PackageName"]??"com.inkroute.app";var expected=dto.ProductKind=="ai_project_pass"?config["GooglePlay:AiProjectPassProductId"]:config["GooglePlay:ArtistSubscriptionProductId"];if(string.IsNullOrWhiteSpace(expected)||dto.ProductId!=expected)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Unexpected Google Play product.");
  var token=dto.PurchasePayload?.Trim();if(string.IsNullOrWhiteSpace(token))return ResultService<MobilePurchaseVerificationResultDto>.Fail("Purchase token is required.");
  var path=config["GooglePlay:ServiceAccountJsonPath"];if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path)||!System.IO.File.Exists(path))return ResultService<MobilePurchaseVerificationResultDto>.Fail("Google Play verification is not configured.");
  var sourceCredential=CredentialFactory.FromFile(path,JsonCredentialParameters.ServiceAccountCredentialType);var credential=sourceCredential.CreateScoped("https://www.googleapis.com/auth/androidpublisher");var access=await ((Google.Apis.Auth.OAuth2.ITokenAccess)credential.UnderlyingCredential).GetAccessTokenForRequestAsync();var http=clients.CreateClient();
  var endpoint=dto.ProductKind=="ai_project_pass"?$"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{package}/purchases/products/{Uri.EscapeDataString(expected)}/tokens/{Uri.EscapeDataString(token)}":$"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{package}/purchases/subscriptionsv2/tokens/{Uri.EscapeDataString(token)}";
  using var req=new HttpRequestMessage(HttpMethod.Get,endpoint);req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",access);var response=await http.SendAsync(req);if(!response.IsSuccessStatusCode)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Google Play did not verify the purchase.");using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
  if(dto.ProductKind=="ai_project_pass"){var user=await db.Users.FirstOrDefaultAsync(x=>x.Id==userId);var bound=doc.RootElement.TryGetProperty("obfuscatedExternalAccountId",out var account)?account.GetString():null;if(user==null||bound!=user.GoogleBillingObfuscatedAccountId)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Google Play account binding mismatch.");if(doc.RootElement.TryGetProperty("purchaseState",out var purchaseState)&&purchaseState.GetInt32()!=0)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Google Play purchase is not completed.");var sandbox=doc.RootElement.TryGetProperty("purchaseType",out var purchaseType)&&purchaseType.GetInt32()==0;var transactionId=doc.RootElement.GetProperty("orderId").GetString()??protector.Hash(token);var prepared=await PrepareAiPurchaseAsync(userId,dto,transactionId,token,sandbox,AiPurchaseStates.ConsumptionPending);if(!prepared.Success)return prepared;using var consumeReq=new HttpRequestMessage(HttpMethod.Post,$"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{package}/purchases/products/{Uri.EscapeDataString(expected)}/tokens/{Uri.EscapeDataString(token)}:consume");consumeReq.Headers.Authorization=new AuthenticationHeaderValue("Bearer",access);consumeReq.Content=new StringContent("{}",Encoding.UTF8,"application/json");var consumeResult=await http.SendAsync(consumeReq);if(!consumeResult.IsSuccessStatusCode&&consumeResult.StatusCode!=System.Net.HttpStatusCode.Conflict)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Google Play AI purchase consumption failed.");return await FinalizeAiPurchaseAsync(userId,dto,transactionId,token,sandbox,AiPurchaseStates.Consumed);}
  var root=doc.RootElement;if(root.TryGetProperty("packageName",out var packageEl)&&packageEl.GetString()!=package)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Google Play package mismatch.");var line=root.GetProperty("lineItems").EnumerateArray().FirstOrDefault(x=>x.GetProperty("productId").GetString()==expected);if(line.ValueKind==JsonValueKind.Undefined)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Google Play product mismatch.");
  var aggregate=await db.ArtistSubscriptions.FirstAsync(x=>x.TattooArtist.UserId==userId);var obfuscated=root.TryGetProperty("externalAccountIdentifiers",out var ids)&&ids.TryGetProperty("obfuscatedExternalAccountId",out var oid)?oid.GetString():null;if(obfuscated!=aggregate.GoogleObfuscatedAccountId)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Google Play account binding mismatch.");
  var expiry=DateTime.Parse(line.GetProperty("expiryTime").GetString()!,null,System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();var state=root.GetProperty("subscriptionState").GetString();var normalized=state switch{"SUBSCRIPTION_STATE_ACTIVE"=>ArtistSubscriptionStatuses.Active,"SUBSCRIPTION_STATE_IN_GRACE_PERIOD"=>ArtistSubscriptionStatuses.GracePeriod,"SUBSCRIPTION_STATE_ON_HOLD"=>ArtistSubscriptionStatuses.OnHold,"SUBSCRIPTION_STATE_PAUSED"=>ArtistSubscriptionStatuses.Paused,"SUBSCRIPTION_STATE_CANCELED"=>ArtistSubscriptionStatuses.Cancelled,"SUBSCRIPTION_STATE_EXPIRED"=>ArtistSubscriptionStatuses.Expired,"SUBSCRIPTION_STATE_PENDING"=>ArtistSubscriptionStatuses.Pending,_=>ArtistSubscriptionStatuses.Pending};var autoRenew=line.TryGetProperty("autoRenewingPlan",out var plan)&&plan.TryGetProperty("autoRenewEnabled",out var ar)&&ar.GetBoolean();var basePlan=config["GooglePlay:ArtistBasePlanId"];if(string.IsNullOrWhiteSpace(basePlan)||!line.TryGetProperty("offerDetails",out var offer)||!offer.TryGetProperty("basePlanId",out var basePlanId)||basePlanId.GetString()!=basePlan)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Google Play base plan mismatch.");var trialOffer=config["GooglePlay:ArtistTrialOfferId"];var isTrial=!string.IsNullOrWhiteSpace(trialOffer)&&offer.TryGetProperty("offerId",out var offerId)&&offerId.GetString()==trialOffer;var isSandbox=root.TryGetProperty("testPurchase",out var testPurchase)&&testPurchase.ValueKind==JsonValueKind.Object;
  var apply=await subscriptions.ApplyVerifiedProviderAsync(userId,new(){Provider=SubscriptionProviders.GooglePlay,ExternalSubscriptionId=protector.Hash(token),ExternalTransactionId=root.TryGetProperty("latestOrderId",out var order)?order.GetString():null,ProductId=expected,Status=normalized,CurrentPeriodEndsAt=expiry,CancelAtPeriodEnd=!autoRenew,IsSandbox=isSandbox,IsTrial=isTrial,TrialEndsAt=isTrial?expiry:null,RawPurchasePayload=token,ProviderEventAt=UtcNow,ProviderEventId=root.TryGetProperty("latestOrderId",out var orderingOrder)?orderingOrder.GetString():protector.Hash(token)});if(!apply.Success)return ResultService<MobilePurchaseVerificationResultDto>.Fail(apply.ErrorMessage!);
  if(root.TryGetProperty("acknowledgementState",out var ack)&&ack.GetString()=="ACKNOWLEDGEMENT_STATE_PENDING"){using var ackReq=new HttpRequestMessage(HttpMethod.Post,$"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{package}/purchases/subscriptions/{Uri.EscapeDataString(expected)}/tokens/{Uri.EscapeDataString(token)}:acknowledge");ackReq.Headers.Authorization=new AuthenticationHeaderValue("Bearer",access);ackReq.Content=new StringContent("{}",Encoding.UTF8,"application/json");var ackResult=await http.SendAsync(ackReq);if(!ackResult.IsSuccessStatusCode)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Google Play purchase was verified but acknowledgement failed.");}
  var effectiveStatus=isSandbox&&!config.GetValue<bool>("Billing:AllowSandboxEntitlements")?ArtistSubscriptionStatuses.Expired:normalized;return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=ArtistSubscriptionStatuses.GrantsAccess(effectiveStatus)||(effectiveStatus==ArtistSubscriptionStatuses.Cancelled&&expiry>UtcNow),AccessUntil=expiry,IsSandboxEntitlement=isSandbox});
 }

 private async Task<ResultService<MobilePurchaseVerificationResultDto>> VerifyAppleAsync(string userId,MobilePurchaseVerificationDto dto)
 {
  JsonDocument clientDocument;try{clientDocument=appleVerifier.Verify(dto.PurchasePayload);}catch(Exception){return ResultService<MobilePurchaseVerificationResultDto>.Fail("Apple purchase verification failed.");}
  using(clientDocument)
  {
   var client=clientDocument.RootElement;var expectedBundle=config["Apple:BundleId"]??"com.inkroute.app";var expected=dto.ProductKind=="ai_project_pass"?config["Apple:AiProjectPassProductId"]:config["Apple:ArtistSubscriptionProductId"];
   if(string.IsNullOrWhiteSpace(expected)||client.GetProperty("bundleId").GetString()!=expectedBundle||client.GetProperty("productId").GetString()!=expected)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Apple product or bundle mismatch.");
   var appToken=ReadGuid(client,"appAccountToken");var aggregate=await db.ArtistSubscriptions.FirstOrDefaultAsync(x=>x.TattooArtist.UserId==userId);
   if(dto.ProductKind=="ai_project_pass"){var user=await db.Users.FirstOrDefaultAsync(x=>x.Id==userId);if(user==null||appToken!=user.AppleBillingAppAccountToken)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Apple account binding mismatch.");}else if(aggregate==null||appToken!=aggregate.AppleAppAccountToken)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Apple account binding mismatch.");
   var transactionId=client.GetProperty("transactionId").GetString()!;var clientOriginal=ReadString(client,"originalTransactionId")??transactionId;var isOneTime=dto.ProductKind=="ai_project_pass";
   var serverResponse=isOneTime?await appleApi.GetTransactionAsync(transactionId,false):await appleApi.GetStatusAsync(transactionId,false);var sandbox=false;if(serverResponse==null){serverResponse=isOneTime?await appleApi.GetTransactionAsync(transactionId,true):await appleApi.GetStatusAsync(transactionId,true);sandbox=true;}if(serverResponse==null)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Apple Server API did not confirm the transaction.");
   var confirmed=SelectAppleState(serverResponse,expectedBundle,expected,clientOriginal,appToken,sandbox);if(confirmed==null)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Apple Server API transaction mismatch.");
   using(confirmed.Transaction)using(confirmed.Renewal)
   {
    var transaction=confirmed.Transaction.RootElement;var confirmedEnvironment=ReadString(transaction,"environment");var clientEnvironment=ReadString(client,"environment");if(!string.IsNullOrWhiteSpace(clientEnvironment)&&clientEnvironment!=confirmedEnvironment)return ResultService<MobilePurchaseVerificationResultDto>.Fail("Apple Server API transaction mismatch.");
    if(isOneTime)
    {
     var verifiedTransactionId=transaction.GetProperty("transactionId").GetString()!;
     if(AppleAiPurchaseGrantabilityRules.Evaluate(transaction)==AppleAiPurchaseGrantability.ValidButRevoked)
     {
      var recorded=await PrepareAiPurchaseAsync(userId,dto,verifiedTransactionId,dto.PurchasePayload,sandbox,AiPurchaseStates.Revoked);
      if(!recorded.Success)return recorded;
      return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=false,IsSandboxEntitlement=sandbox});
     }
     return await FinalizeAiPurchaseAsync(userId,dto,verifiedTransactionId,dto.PurchasePayload,sandbox,AiPurchaseStates.Verified);
    }
    var expiry=ReadAppleDate(transaction,"expiresDate");var renewal=confirmed.Renewal?.RootElement;var graceEnd=renewal.HasValue?ReadAppleDate(renewal.Value,"gracePeriodExpiresDate"):null;var status=MapAppleServerStatus(confirmed.Status,transaction,expiry);var periodEnd=status==ArtistSubscriptionStatuses.GracePeriod?graceEnd??expiry:expiry;var cancelAtPeriodEnd=renewal.HasValue&&ReadInt(renewal.Value,"autoRenewStatus")==0;var isTrial=ReadInt(transaction,"offerType")==1;
    var result=await subscriptions.ApplyVerifiedProviderAsync(userId,new(){Provider=SubscriptionProviders.Apple,ExternalSubscriptionId=ReadString(transaction,"originalTransactionId")!,ExternalTransactionId=transaction.GetProperty("transactionId").GetString(),ProductId=expected,Status=status,CurrentPeriodEndsAt=periodEnd,CancelAtPeriodEnd=cancelAtPeriodEnd,IsSandbox=sandbox,IsTrial=isTrial,TrialEndsAt=isTrial?expiry:null,RawPurchasePayload=dto.PurchasePayload,ProviderEventAt=ReadAppleDate(transaction,"signedDate")??ReadAppleDate(transaction,"purchaseDate"),ProviderEventId=transaction.GetProperty("transactionId").GetString()});if(!result.Success)return ResultService<MobilePurchaseVerificationResultDto>.Fail(result.ErrorMessage!);var normalized=SubscriptionEntitlementRules.Normalize(status,isTrial?expiry:null,periodEnd,cancelAtPeriodEnd,isTrial,UtcNow);var effectiveStatus=sandbox&&!config.GetValue<bool>("Billing:AllowSandboxEntitlements")?ArtistSubscriptionStatuses.Expired:normalized;return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=ArtistSubscriptionStatuses.GrantsAccess(effectiveStatus),AccessUntil=periodEnd,IsSandboxEntitlement=sandbox});
   }
  }
 }

 private sealed record AppleVerifiedState(JsonDocument Transaction,JsonDocument? Renewal,int? Status);

 private AppleVerifiedState? SelectAppleState(string json,string bundle,string product,string original,Guid? appToken,bool sandbox)
 {
  using var response=JsonDocument.Parse(json);var candidates=new List<(string Transaction,string? Renewal,int? Status)>();CollectAppleStates(response.RootElement,candidates);AppleVerifiedState? selected=null;long selectedDate=long.MinValue;
  foreach(var candidate in candidates){JsonDocument? transaction=null;JsonDocument? renewal=null;try{transaction=appleVerifier.Verify(candidate.Transaction);var tr=transaction.RootElement;var transactionOriginal=ReadString(tr,"originalTransactionId")??ReadString(tr,"transactionId");if(transactionOriginal!=original||ReadString(tr,"bundleId")!=bundle||ReadString(tr,"productId")!=product||ReadGuid(tr,"appAccountToken")!=appToken||ReadString(tr,"environment")!=(sandbox?"Sandbox":"Production")){transaction.Dispose();continue;}if(candidate.Renewal!=null){renewal=appleVerifier.Verify(candidate.Renewal);var rn=renewal.RootElement;if(ReadString(rn,"originalTransactionId")!=original||ReadString(rn,"productId")!=product||ReadString(rn,"environment")!=(sandbox?"Sandbox":"Production")){transaction.Dispose();renewal.Dispose();continue;}}var signedDate=ReadLong(tr,"signedDate")??ReadLong(tr,"purchaseDate")??0;if(signedDate>=selectedDate){selected?.Transaction.Dispose();selected?.Renewal?.Dispose();selectedDate=signedDate;selected=new(transaction,renewal,candidate.Status);}else{transaction.Dispose();renewal?.Dispose();}}catch{transaction?.Dispose();renewal?.Dispose();}}
  return selected;
 }

 private static void CollectAppleStates(JsonElement element,List<(string Transaction,string? Renewal,int? Status)> results)
 {
  if(element.ValueKind==JsonValueKind.Object){if(element.TryGetProperty("signedTransactionInfo",out var signed)&&signed.ValueKind==JsonValueKind.String&&signed.GetString() is string transaction){var renewal=element.TryGetProperty("signedRenewalInfo",out var renewalElement)&&renewalElement.ValueKind==JsonValueKind.String?renewalElement.GetString():null;var status=element.TryGetProperty("status",out var statusElement)&&statusElement.TryGetInt32(out var statusValue)?statusValue:(int?)null;results.Add((transaction,renewal,status));}foreach(var property in element.EnumerateObject())CollectAppleStates(property.Value,results);}else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray())CollectAppleStates(item,results);
 }

 private string MapAppleServerStatus(int? serverStatus,JsonElement transaction,DateTime? expiry)=>serverStatus switch{1=>ArtistSubscriptionStatuses.Active,2=>ArtistSubscriptionStatuses.Expired,3=>ArtistSubscriptionStatuses.PastDue,4=>ArtistSubscriptionStatuses.GracePeriod,5=>ArtistSubscriptionStatuses.Revoked,_=>transaction.TryGetProperty("revocationDate",out var revoked)&&revoked.ValueKind==JsonValueKind.Number?ArtistSubscriptionStatuses.Revoked:expiry>UtcNow?ArtistSubscriptionStatuses.Active:ArtistSubscriptionStatuses.Expired};
 private static string? ReadString(JsonElement element,string name)=>element.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString():null;
 private static Guid? ReadGuid(JsonElement element,string name)=>Guid.TryParse(ReadString(element,name),out var value)?value:null;
 private static int? ReadInt(JsonElement element,string name)=>element.TryGetProperty(name,out var value)&&value.TryGetInt32(out var result)?result:null;
 private static long? ReadLong(JsonElement element,string name)=>element.TryGetProperty(name,out var value)&&value.TryGetInt64(out var result)?result:null;
 private static DateTime? ReadAppleDate(JsonElement element,string name)=>ReadLong(element,name) is long milliseconds?DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime:null;
 private async Task<ResultService<MobilePurchaseVerificationResultDto>> PrepareAiPurchaseAsync(string userId,MobilePurchaseVerificationDto dto,string transactionId,string raw,bool sandbox,string state)
 {
  if(dto.AiProjectId==null)return ResultService<MobilePurchaseVerificationResultDto>.Fail("AI project is required.");
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
  await DatabaseApplicationLock.AcquireAsync(db,$"InkRoute:AiPass:{dto.AiProjectId.Value}","ai_pass_concurrency_conflict","The AI Project Pass is being updated by another request. Retry verification.");
  db.ChangeTracker.Clear();
  var project=await db.AiTattooProjects.FirstOrDefaultAsync(x=>x.Id==dto.AiProjectId&&x.UserId==userId);
  if(project==null||project.IsFreeProject){await tx.RollbackAsync();return ResultService<MobilePurchaseVerificationResultDto>.Fail("A paid AI draft owned by the user is required.");}
  var hash=protector.Hash(raw);
  var existing=await db.AiProjectStorePurchases.FirstOrDefaultAsync(x=>x.PurchasePayloadHash==hash||(x.Provider==dto.Provider&&x.ExternalTransactionId==transactionId));
  if(existing!=null)
  {
   if(existing.UserId!=userId||existing.AiTattooProjectId!=project.Id||existing.Provider!=dto.Provider||existing.ProductId!=dto.ProductId){await tx.RollbackAsync();return ResultService<MobilePurchaseVerificationResultDto>.Fail("This purchase has already been linked.");}
   if(existing.ProcessingState==AiPurchaseStates.Granted){await tx.CommitAsync();return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=existing.AccessGrantedUntil>UtcNow,AccessUntil=existing.AccessGrantedUntil,IsSandboxEntitlement=existing.IsSandbox});}
   if(existing.ProcessingState is AiPurchaseStates.Revoked or AiPurchaseStates.Invalid or AiPurchaseStates.SandboxVerified)
   {
    await tx.CommitAsync();return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=false,IsSandboxEntitlement=existing.IsSandbox});
   }
   if(!AiPurchaseStateRules.CanTransition(existing.ProcessingState,state)){await tx.RollbackAsync();return ResultService<MobilePurchaseVerificationResultDto>.Fail("Purchase processing state cannot move backwards.");}
   existing.ProcessingState=state;existing.LastAttemptAtUtc=UtcNow;existing.LastErrorCode=null;
   await db.SaveChangesAsync();await tx.CommitAsync();return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=false,IsSandboxEntitlement=sandbox});
  }
  db.AiProjectStorePurchases.Add(new(){AiTattooProjectId=project.Id,UserId=userId,Provider=dto.Provider,ExternalTransactionId=transactionId,ProductId=dto.ProductId,PurchasePayloadHash=hash,EncryptedPurchasePayload=protector.Protect(raw),IsSandbox=sandbox,ProcessingState=state,VerifiedAt=UtcNow,AccessGrantedUntil=UtcNow,LastAttemptAtUtc=UtcNow});
  await db.SaveChangesAsync();await tx.CommitAsync();
  return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=false,IsSandboxEntitlement=sandbox});
 }

 private async Task<ResultService<MobilePurchaseVerificationResultDto>> FinalizeAiPurchaseAsync(string userId,MobilePurchaseVerificationDto dto,string transactionId,string raw,bool sandbox,string verifiedState)
 {
  var prepared=await PrepareAiPurchaseAsync(userId,dto,transactionId,raw,sandbox,verifiedState);if(!prepared.Success)return prepared;
  var hash=protector.Hash(raw);var purchase=await db.AiProjectStorePurchases.FirstAsync(x=>x.PurchasePayloadHash==hash&&x.Provider==dto.Provider&&x.ExternalTransactionId==transactionId);
  if(purchase.ProcessingState==AiPurchaseStates.Granted)return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=purchase.AccessGrantedUntil>UtcNow,AccessUntil=purchase.AccessGrantedUntil,IsSandboxEntitlement=purchase.IsSandbox});
  if(purchase.ProcessingState is AiPurchaseStates.Revoked or AiPurchaseStates.Invalid or AiPurchaseStates.SandboxVerified)return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=false,IsSandboxEntitlement=purchase.IsSandbox});
  var allowSandbox=config.GetValue<bool>("Billing:AllowSandboxEntitlements");
  if(!BillingSandboxPolicy.CanGrant(sandbox,allowSandbox))
  {
   if(AiPurchaseStateRules.CanTransition(purchase.ProcessingState,AiPurchaseStates.SandboxVerified))purchase.ProcessingState=AiPurchaseStates.SandboxVerified;
   purchase.LastAttemptAtUtc=UtcNow;purchase.LastErrorCode=null;await db.SaveChangesAsync();
   return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=false,IsSandboxEntitlement=sandbox});
  }
  if(purchase.AiTattooProjectId==null)return ResultService<MobilePurchaseVerificationResultDto>.Fail("The linked AI project no longer exists.");
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
  await DatabaseApplicationLock.AcquireAsync(db,$"InkRoute:AiPass:{purchase.AiTattooProjectId}","ai_pass_concurrency_conflict","The AI Project Pass is being updated by another request. Retry verification.");
  db.ChangeTracker.Clear();
  purchase=await db.AiProjectStorePurchases.FirstAsync(x=>x.Id==purchase.Id);
  if(purchase.ProcessingState==AiPurchaseStates.Granted){await tx.CommitAsync();return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=purchase.AccessGrantedUntil>UtcNow,AccessUntil=purchase.AccessGrantedUntil,IsSandboxEntitlement=purchase.IsSandbox});}
  if(purchase.ProcessingState is AiPurchaseStates.Revoked or AiPurchaseStates.Invalid or AiPurchaseStates.SandboxVerified){await tx.CommitAsync();return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=false,IsSandboxEntitlement=purchase.IsSandbox});}
  if(!AiPurchaseStateRules.CanTransition(purchase.ProcessingState,AiPurchaseStates.GrantPending)){await tx.RollbackAsync();return ResultService<MobilePurchaseVerificationResultDto>.Fail("Purchase processing state cannot be granted.");}
  purchase.ProcessingState=AiPurchaseStates.GrantPending;purchase.LastAttemptAtUtc=UtcNow;purchase.LastErrorCode=null;
  var project=await db.AiTattooProjects.FirstOrDefaultAsync(x=>x.Id==purchase.AiTattooProjectId&&x.UserId==userId);
  if(project==null){await tx.RollbackAsync();return ResultService<MobilePurchaseVerificationResultDto>.Fail("AI project was not found.");}
  var now=UtcNow;var until=AiPassGrantRules.CalculateEnd(now,project.EditingAccessUntil);project.EditingAccessUntil=until;project.UpdatedAt=now;purchase.AccessGrantedUntil=until;purchase.ProcessingState=AiPurchaseStates.Granted;purchase.LastAttemptAtUtc=now;
  await db.SaveChangesAsync();await tx.CommitAsync();return ResultService<MobilePurchaseVerificationResultDto>.Ok(new(){Verified=true,HasAccess=true,AccessUntil=until,IsSandboxEntitlement=sandbox});
 }

 public async Task<ResultService> ProcessGoogleNotificationAsync(string payload,string authorization,string expectedAudience)
 {
  try{if(string.IsNullOrWhiteSpace(expectedAudience)||string.IsNullOrWhiteSpace(config["GooglePlay:PubSubServiceAccountEmail"]))return ResultService.Fail("Google Pub/Sub verification is not configured.");var bearer=authorization.StartsWith("Bearer ",StringComparison.OrdinalIgnoreCase)?authorization[7..]:"";var oidc=await GoogleJsonWebSignature.ValidateAsync(bearer,new(){Audience=[expectedAudience]});if(oidc.EmailVerified!=true||oidc.Email!=config["GooglePlay:PubSubServiceAccountEmail"])return ResultService.Fail("Google Pub/Sub identity is invalid.");var push=JsonSerializer.Deserialize<GooglePubSubPushDto>(payload,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??throw new InvalidOperationException("Invalid Pub/Sub payload.");var decoded=Encoding.UTF8.GetString(Convert.FromBase64String(push.Message.Data));using var doc=JsonDocument.Parse(decoded);var root=doc.RootElement;if(root.GetProperty("packageName").GetString()!=(config["GooglePlay:PackageName"]??"com.inkroute.app"))return ResultService.Fail("Google package mismatch.");return await events.ExecuteOnceAsync(SubscriptionProviders.GooglePlay,push.Message.MessageId,"rtdn",payload,async()=>{if(root.TryGetProperty("subscriptionNotification",out var notification)){var token=notification.GetProperty("purchaseToken").GetString()!;var product=notification.GetProperty("subscriptionId").GetString()!;var existing=await db.ProviderSubscriptions.Include(x=>x.ArtistSubscription).ThenInclude(x=>x.TattooArtist).FirstOrDefaultAsync(x=>x.PurchaseTokenHash==protector.Hash(token));if(existing==null)return ResultService.Fail("Purchase is not linked yet; retry notification.");var dto=new MobilePurchaseVerificationDto{Provider=SubscriptionProviders.GooglePlay,ProductId=product,PurchasePayload=token};return (await VerifyGoogleAsync(existing.ArtistSubscription.TattooArtist.UserId,dto)).Success?ResultService.Ok():ResultService.Fail("RTDN verification failed.");}if(root.TryGetProperty("voidedPurchaseNotification",out var voided)){var token=voided.GetProperty("purchaseToken").GetString()!;var existing=await db.ProviderSubscriptions.FirstOrDefaultAsync(x=>x.PurchaseTokenHash==protector.Hash(token));if(existing==null)return ResultService.Fail("Purchase is not linked yet; retry notification.");return await subscriptions.ApplyVerifiedProviderAsync(null,new(){Provider=SubscriptionProviders.GooglePlay,ExternalSubscriptionId=existing.ExternalSubscriptionId,ProductId=existing.ProductId,Status=ArtistSubscriptionStatuses.Revoked,IsSandbox=existing.IsSandbox,RawPurchasePayload=token,ProviderEventAt=UtcNow,ProviderEventId=root.TryGetProperty("latestOrderId",out var orderingOrder)?orderingOrder.GetString():protector.Hash(token)});}return ResultService.Ok();});}catch(Exception){return ResultService.Fail("Google notification verification failed.");}
 }

 public async Task<ResultService> ProcessAppleNotificationAsync(string signedPayload)
 {
  try
  {
   using var doc=appleVerifier.Verify(signedPayload);var root=doc.RootElement;var id=ReadString(root,"notificationUUID");var type=ReadString(root,"notificationType");if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(type))return ResultService.Fail("Apple notification is invalid.");
   return await events.ExecuteOnceAsync(SubscriptionProviders.Apple,id,type,signedPayload,async()=>
   {
    if(!root.TryGetProperty("data",out var data)||!data.TryGetProperty("signedTransactionInfo",out var signed)||signed.ValueKind!=JsonValueKind.String)return ResultService.Ok();
    using var tx=appleVerifier.Verify(signed.GetString()!);var tr=tx.RootElement;var expectedBundle=config["Apple:BundleId"]??"com.inkroute.app";var expectedProduct=config["Apple:ArtistSubscriptionProductId"];var environment=ReadString(data,"environment");
    if(ReadString(tr,"productId")!=expectedProduct)
    {
     // Notifications for the separate AI consumable must not mutate the artist entitlement.
     return ResultService.Ok();
    }
    if(ReadString(tr,"bundleId")!=expectedBundle||string.IsNullOrWhiteSpace(expectedProduct)||environment is not ("Sandbox" or "Production")||ReadString(tr,"environment")!=environment)return ResultService.Fail("Apple notification transaction mismatch.");
    var original=ReadString(tr,"originalTransactionId");if(string.IsNullOrWhiteSpace(original))return ResultService.Fail("Apple notification transaction mismatch.");var existing=await db.ProviderSubscriptions.Include(x=>x.ArtistSubscription).FirstOrDefaultAsync(x=>x.Provider==SubscriptionProviders.Apple&&x.ExternalSubscriptionId==original);if(existing==null)return ResultService.Fail("Purchase is not linked yet; retry notification.");if(ReadGuid(tr,"appAccountToken")!=existing.ArtistSubscription.AppleAppAccountToken)return ResultService.Fail("Apple notification account binding mismatch.");
    JsonDocument? renewalDocument=null;try
    {
     if(data.TryGetProperty("signedRenewalInfo",out var signedRenewal)&&signedRenewal.ValueKind==JsonValueKind.String)
     {
      renewalDocument=appleVerifier.Verify(signedRenewal.GetString()!);var renewal=renewalDocument.RootElement;if(ReadString(renewal,"originalTransactionId")!=original||ReadString(renewal,"productId")!=expectedProduct||ReadString(renewal,"environment")!=environment)return ResultService.Fail("Apple notification renewal mismatch.");
     }
     var renewalElement=renewalDocument?.RootElement;var expiry=ReadAppleDate(tr,"expiresDate");var graceEnd=renewalElement.HasValue?ReadAppleDate(renewalElement.Value,"gracePeriodExpiresDate"):null;var existingCancel=existing.CancelAtPeriodEnd;var cancelAtPeriodEnd=renewalElement.HasValue?ReadInt(renewalElement.Value,"autoRenewStatus")==0:existingCancel;var subtype=ReadString(root,"subtype");var isTrial=ReadInt(tr,"offerType")==1;
     var status=type switch
     {
      "REFUND" or "REVOKE"=>ArtistSubscriptionStatuses.Revoked,
      "EXPIRED" or "GRACE_PERIOD_EXPIRED"=>ArtistSubscriptionStatuses.Expired,
      "DID_FAIL_TO_RENEW" when subtype=="GRACE_PERIOD"=>ArtistSubscriptionStatuses.GracePeriod,
      "DID_FAIL_TO_RENEW"=>ArtistSubscriptionStatuses.PastDue,
      "DID_RENEW" or "SUBSCRIBED" or "RENEWAL_EXTENDED"=>isTrial?ArtistSubscriptionStatuses.Trialing:ArtistSubscriptionStatuses.Active,
      "DID_CHANGE_RENEWAL_STATUS"=>expiry>UtcNow?(isTrial?ArtistSubscriptionStatuses.Trialing:ArtistSubscriptionStatuses.Active):ArtistSubscriptionStatuses.Expired,
      _=>expiry>UtcNow?(isTrial?ArtistSubscriptionStatuses.Trialing:ArtistSubscriptionStatuses.Active):ArtistSubscriptionStatuses.Expired
     };
     var periodEnd=status==ArtistSubscriptionStatuses.GracePeriod?graceEnd??expiry:expiry;
     return await subscriptions.ApplyVerifiedProviderAsync(null,new(){Provider=SubscriptionProviders.Apple,ExternalSubscriptionId=original,ExternalTransactionId=ReadString(tr,"transactionId"),ProductId=expectedProduct,Status=status,CurrentPeriodEndsAt=periodEnd,CancelAtPeriodEnd=cancelAtPeriodEnd,IsSandbox=environment=="Sandbox",IsTrial=isTrial,TrialEndsAt=isTrial?expiry:null,RawPurchasePayload=signed.GetString(),ProviderEventAt=ReadAppleDate(root,"signedDate")??ReadAppleDate(tr,"signedDate"),ProviderEventId=id});
    }
    finally{renewalDocument?.Dispose();}
   });
  }
  catch(Exception){return ResultService.Fail("Apple notification verification failed.");}
 }
}
