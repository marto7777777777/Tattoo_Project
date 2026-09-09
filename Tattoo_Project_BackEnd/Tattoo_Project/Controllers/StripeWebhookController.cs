using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.Services.Interfaces;
namespace Tattoo_Project.Controllers;
[ApiController,Route("api/stripe/webhook")]
public class StripeWebhookController(IStripeWebhookService service):ControllerBase
{
 [HttpPost]public async Task<IActionResult> Post(){using var reader=new StreamReader(Request.Body);var payload=await reader.ReadToEndAsync();var result=await service.ProcessAsync(payload,Request.Headers["Stripe-Signature"].ToString());return result.Success?Ok():BadRequest(result.ErrorMessage);}
}
