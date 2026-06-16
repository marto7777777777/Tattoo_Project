using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.TattooRequestDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TattooRequestController(ITattooRequestService service)
        : ControllerBase
    {
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAllTattooRequests()
        {
            var tattooRequests = await service.GetAllTattooRequestsAsync();

            return Ok(tattooRequests);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin + "," + UserRoles.Client + "," + UserRoles.TattooArtist)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTattooRequestById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var tattooRequest = await service.GetTattooRequestByIdAsync(
                id,
                userId,
                User.IsInRole(UserRoles.Admin),
                User.IsInRole(UserRoles.Client),
                User.IsInRole(UserRoles.TattooArtist));

            if (tattooRequest == null)
            {
                return NotFound("Tattoo request not found.");
            }

            return Ok(tattooRequest);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Client)]
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyTattooRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var tattooRequests = await service.GetMyTattooRequestsAsync(userId);

            return Ok(tattooRequests);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Client)]
        [HttpPost]
        public async Task<IActionResult> CreateTattooRequest(
            CreateTattooRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isCreated = await service.CreateTattooRequest(dto, userId);

            if (!isCreated)
            {
                return BadRequest("Tattoo request could not be created.");
            }

            return Ok("Tattoo request created successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Client)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTattooRequest(
            int id,
            UpdateTattooRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isUpdated = await service.UpdateTattooRequestAsync(
                id,
                dto,
                userId);

            if (!isUpdated)
            {
                return BadRequest("Tattoo request could not be updated.");
            }

            return Ok("Tattoo request updated successfully.");
        }
    }
}