using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.TattooArtistDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TattooArtistController(
        ITattooArtistService service,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService)
        : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllTattooArtists()
        {
            var tattooArtists = await service.GetAllTattooArtistsAsync();

            return Ok(tattooArtists);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTattooArtistById(int id)
        {
            var tattooArtist = await service.GetTattooArtistByIdAsync(id);

            if (tattooArtist == null)
            {
                return NotFound("Tattoo artist not found.");
            }

            return Ok(tattooArtist);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("profile")]
        public async Task<IActionResult> CreateTattooArtistProfile(
            CreateTattooArtistDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isCreated = await service.CreateTattooArtistProfileAsync(
                dto,
                userId);

            if (!isCreated)
            {
                return BadRequest("Tattoo artist profile already exists or invalid data.");
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            var token = await tokenService.GenerateJwtTokenAsync(user);

            return Ok(new
            {
                Message = "Tattoo artist profile created successfully.",
                Token = token
            });
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateTattooArtistProfile(
            UpdateArtistDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isUpdated = await service.UpdateTattooArtistProfileAsync(
                dto,
                userId);

            if (!isUpdated)
            {
                return BadRequest("Tattoo artist profile could not be updated.");
            }

            return Ok("Tattoo artist profile updated successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTattooArtist(int id)
        {
            var isDeleted = await service.DeleteTattooArtistAsync(id);

            if (!isDeleted)
            {
                return NotFound("Tattoo artist not found.");
            }

            return Ok("Tattoo artist deleted successfully.");
        }
    }
}