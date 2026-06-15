using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.ArtistResponseDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArtistResponseController(IArtistResponseService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<GetArtistResponseDto>>> GetAllArtistResponsesAsync()
        {
            var artistsResponses = await service.GetAllArtistResponsesAsync();
            if (artistsResponses == null || !artistsResponses.Any())
            {
                return NotFound($"No artist responses yet!");
            }
            return Ok(artistsResponses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GetArtistResponseDto>> GetArtistResponseByIdAsync(int id)
        {
            var artist = await service.GetArtistResponseByIdAsync(id);
            if (artist == null)
            {
                return NotFound($"Artist response with id {id} doesn't exist!");
            }
            return Ok(artist);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
        [HttpPost]
        public async Task<IActionResult> CreateArtistResponse(CreateArtistResponseDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isCreated = await service.CreateArtistResponseAsync(dto, userId);

            if (!isCreated)
            {
                return BadRequest("Artist response could not be created.");
            }

            return Ok("Artist response created successfully.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArtistResponse(int id)
        {
            var isDeleted = await service.DeleteArtistResponseAsync(id);
            return Ok(isDeleted);
        }

        [Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = UserRoles.TattooArtist)]
        [HttpPut("reject-tattoo-request/{tattooRequestId}")]
        public async Task<IActionResult> RejectTattooRequest(int tattooRequestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isRejected = await service.RejectTattooRequestAsync(
                tattooRequestId,
                userId);

            if (!isRejected)
            {
                return BadRequest("Tattoo request could not be rejected.");
            }

            return Ok("Tattoo request rejected successfully.");
        }
    }
}
