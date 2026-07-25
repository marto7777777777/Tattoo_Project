using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Client)]
    public class ClientFavoriteStudioController(IClientFavoriteStudioService service) : ControllerBase
    {
        [HttpPost("{studioId:int}")]
        public async Task<IActionResult> Add(int studioId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            var result = await service.AddAsync(studioId, userId);
            return result.Success ? Ok("Studio added to favorites.") : BadRequest(result.ErrorMessage);
        }

        [HttpDelete("{studioId:int}")]
        public async Task<IActionResult> Remove(int studioId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            var result = await service.RemoveAsync(studioId, userId);
            return result.Success ? Ok("Studio removed from favorites.") : BadRequest(result.ErrorMessage);
        }

        [HttpGet("my-favorites")]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            var result = await service.GetMineAsync(userId);
            return result.Success ? Ok(result.Data) : BadRequest(result.ErrorMessage);
        }
    }
}
