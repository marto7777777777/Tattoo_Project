using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.ArtistUnavailableDateDTOs;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Models;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
    public class ArtistUnavailableDateController : ControllerBase
    {
        private readonly IArtistUnavailableDateService artistUnavailableDateService;

        public ArtistUnavailableDateController(IArtistUnavailableDateService artistUnavailableDateService)
        {
            this.artistUnavailableDateService = artistUnavailableDateService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUnavailableDate(CreateArtistUnavailableDateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await artistUnavailableDateService.CreateUnavailableDateAsync(dto, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Unavailable period created successfully.");
        }

        [HttpGet("my-unavailable-dates")]
        public async Task<IActionResult> GetMyUnavailableDates()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await artistUnavailableDateService.GetMyUnavailableDatesAsync(userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUnavailableDate(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await artistUnavailableDateService.DeleteUnavailableDateAsync(id, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Unavailable period deleted successfully.");
        }
    }
}