using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.TattooArtistDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services;
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
            var result = await service.GetAllTattooArtistsAsync();

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTattooArtistById(int id)
        {
            var result = await service.GetTattooArtistByIdAsync(id);

            if (!result.Success)
            {
                return NotFound(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [HttpGet("recommended")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetRecommendedTattooArtists()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.GetRecommendedTattooArtistsAsync(userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTattooArtists([FromQuery] string query)
        {
            var result = await service.SearchTattooArtistsAsync(query);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
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

            var result = await service.CreateTattooArtistProfileAsync(
                dto,
                userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
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

            var result = await service.UpdateTattooArtistProfileAsync(
                dto,
                userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Tattoo artist profile updated successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTattooArtist(int id)
        {
            var result = await service.DeleteTattooArtistAsync(id);

            if (!result.Success)
            {
                return NotFound(result.ErrorMessage);
            }

            return Ok("Tattoo artist deleted successfully.");
        }
    }
}