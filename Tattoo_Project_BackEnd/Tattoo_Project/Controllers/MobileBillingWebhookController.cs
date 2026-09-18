using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.RateLimiting;using Tattoo_Project.DTOs.SubscriptionDTOs;using Tattoo_Project.Services.Interfaces;
namespace Tattoo_Project.Controllers;
[ApiController,Route("api/subscription/webhooks")]
[EnableRateLimiting("billing-webhook")]
[RequestSizeLimit(1_048_576)]
public class MobileBillingWebhookController(IMobileBillingService service,IConfiguration config):ControllerBase
{
 [AllowAnonymous,HttpPost("google")]public async Task<IActionResult> Google(){using var reader=new StreamReader(Request.Body);var raw=await reader.ReadToEndAsync();var result=await service.ProcessGoogleNotificationAsync(raw,Request.Headers.Authorization.ToString(),config["GooglePlay:PubSubAudience"]??"");return result.Success?Ok():Unauthorized(result.ErrorMessage);}
 [AllowAnonymous,HttpPost("apple")]public async Task<IActionResult> Apple([FromBody]AppleNotificationDto dto){var result=await service.ProcessAppleNotificationAsync(dto.SignedPayload);return result.Success?Ok():BadRequest(result.ErrorMessage);}
}
