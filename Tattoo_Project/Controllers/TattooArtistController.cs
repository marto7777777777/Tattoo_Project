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
    [ApiController]
    [Route("api/[controller]")]
    public class TattooArtistController(
    ITattooArtistService service,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService)
    : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<GetTattooArtistDto>>> GetArtists()
        {
            var tattooArtists = await service.GetAllArtistsAsync();
            if (tattooArtists is null || !tattooArtists.Any())
            {
               return NotFound("No artists yet!");
            }
            return Ok(tattooArtists);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GetTattooArtistDto>> GetArtistByIdAsync(int id)
        {
            var tattooArtist = await service.GetTattooArtistByIdAsync(id);
            if (tattooArtist is null)
            {
                return NotFound($"Artist with Id {id} is not found!");
            }
            return Ok(tattooArtist);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("profile")]
        public async Task<IActionResult> CreateTattooArtistProfile(CreateTattooArtistDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.CreateTattooArtistProfileAsync(dto, userId);

            if (!result)
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArtist(int id)
        {
            var isDeleted = await service.DeleteArtist(id);
            if (isDeleted == false)
            {
                return NotFound($"Artist with id {id} already doesn't exist!");
            }
            return Ok(isDeleted);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArtist(int id, UpdateArtistDto dto)
        {
            var isUpdated = await service.UpdateArtist(id, dto);
            return Ok(isUpdated);
        }
    }
}
