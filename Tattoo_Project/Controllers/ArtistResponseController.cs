using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.ArtistResponseDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArtistResponseController(IArtistResponseService service)
        : ControllerBase
    {
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAllArtistResponses()
        {
            var result = await service.GetAllArtistResponsesAsync();

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetArtistResponseById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.GetArtistResponseByIdAsync(id, userId);

            if (!result.Success)
            {
                return NotFound(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
        [HttpGet("my-responses")]
        public async Task<IActionResult> GetMyArtistResponses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.GetMyArtistResponsesAsync(userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
        [HttpPost]
        public async Task<IActionResult> CreateArtistResponse(
            CreateArtistResponseDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.CreateArtistResponseAsync(dto, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Artist response created successfully.");
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

            var result = await service.RejectTattooRequestAsync(
                tattooRequestId,
                userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Tattoo request rejected successfully.");
        }
    }
}