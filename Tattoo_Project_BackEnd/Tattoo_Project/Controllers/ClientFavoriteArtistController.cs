using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Client")]
    public class ClientFavoriteArtistController : ControllerBase
    {
        private readonly IClientFavoriteArtistService clientFavoriteArtistService;

        public ClientFavoriteArtistController(IClientFavoriteArtistService clientFavoriteArtistService)
        {
            this.clientFavoriteArtistService = clientFavoriteArtistService;
        }

        [HttpPost("{tattooArtistId}")]
        public async Task<IActionResult> AddFavoriteArtist(int tattooArtistId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await clientFavoriteArtistService.AddFavoriteArtistAsync(tattooArtistId, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Artist added to favorites.");
        }

        [HttpDelete("{tattooArtistId}")]
        public async Task<IActionResult> RemoveFavoriteArtist(int tattooArtistId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await clientFavoriteArtistService.RemoveFavoriteArtistAsync(tattooArtistId, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Artist removed from favorites.");
        }

        [HttpGet("my-favorites")]
        public async Task<IActionResult> GetMyFavoriteArtists()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await clientFavoriteArtistService.GetMyFavoriteArtistsAsync(userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }
    }
}