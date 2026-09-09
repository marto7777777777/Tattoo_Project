using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.AccountDTOs;
using Tattoo_Project.Services.Interfaces;
namespace Tattoo_Project.Controllers;
[ApiController,Route("api/account"),Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme)]
public class AccountController(IAccountDeletionService service):ControllerBase
{
 [HttpDelete]public async Task<IActionResult> Delete(DeleteAccountDto dto){var r=await service.DeleteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!,dto.Password,dto.Confirmation);return r.Success?NoContent():BadRequest(r.ErrorMessage);}
}
