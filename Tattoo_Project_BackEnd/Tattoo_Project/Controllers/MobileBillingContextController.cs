using Microsoft.AspNetCore.RateLimiting;using System.Security.Claims;using Microsoft.AspNetCore.Authentication.JwtBearer;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Tattoo_Project.Models;using Tattoo_Project.Services.Interfaces;
namespace Tattoo_Project.Controllers;
[ApiController,Route("api/mobile-billing"),Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme,Roles=UserRoles.Admin+","+UserRoles.Client+","+UserRoles.TattooArtist)]
public class MobileBillingContextController(IArtistSubscriptionService subscriptions,IMobileBillingService mobile):ControllerBase
{
 [HttpGet("ai-context")]public async Task<IActionResult> Context([FromQuery]string provider){var r=await subscriptions.GetAiMobileContextAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!,provider);return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);}
 [EnableRateLimiting("sensitive")][HttpPost("verify")]public async Task<IActionResult> Verify([FromBody]Tattoo_Project.DTOs.SubscriptionDTOs.MobilePurchaseVerificationDto dto){if(dto.ProductKind!="ai_project_pass")return BadRequest("Only AI Project Pass purchases can be verified on this endpoint.");var r=await mobile.VerifyAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!,dto);return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);}
}
