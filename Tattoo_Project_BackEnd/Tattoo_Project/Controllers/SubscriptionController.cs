using Microsoft.AspNetCore.RateLimiting;using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.Models;
using Tattoo_Project.DTOs.SubscriptionDTOs;
using Tattoo_Project.Services.Interfaces;
namespace Tattoo_Project.Controllers;
[ApiController,Route("api/subscription"),Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme,Roles=UserRoles.TattooArtist+","+UserRoles.Admin)]
public class SubscriptionController(IArtistSubscriptionService service,IMobileBillingService mobile):ControllerBase
{
 [HttpGet]public async Task<IActionResult> Get(){var id=User.FindFirstValue(ClaimTypes.NameIdentifier)!;var r=await service.GetStatusAsync(id);return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);}
 [EnableRateLimiting("sensitive")][HttpPost("checkout")]public async Task<IActionResult> Checkout(){var r=await service.CreateCheckoutAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);}
 [HttpPost("portal")]public async Task<IActionResult> Portal(){var r=await service.CreatePortalAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);}
 [HttpGet("mobile-context")]public async Task<IActionResult> MobileContext([FromQuery]string provider){var r=await service.GetMobileContextAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!,provider);return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);}
 [EnableRateLimiting("sensitive")][HttpPost("mobile/verify")]public async Task<IActionResult> VerifyMobile([FromBody]MobilePurchaseVerificationDto dto){if(dto.ProductKind!="artist_subscription")return BadRequest("Only artist subscriptions can be verified on this endpoint.");var r=await mobile.VerifyAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!,dto);return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);}
}
