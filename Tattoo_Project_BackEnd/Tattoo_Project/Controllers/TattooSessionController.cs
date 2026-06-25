using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TattooSessionController(ITattooSessionService service)
        : ControllerBase
    {
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAllTattooSessions()
        {
            var result = await service.GetAllTattooSessionsAsync();

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin + "," + UserRoles.Client + "," + UserRoles.TattooArtist)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTattooSessionById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.GetTattooSessionByIdAsync(
                id,
                userId,
                User.IsInRole(UserRoles.Admin),
                User.IsInRole(UserRoles.Client),
                User.IsInRole(UserRoles.TattooArtist));

            if (!result.Success)
            {
                return NotFound(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Client)]
        [HttpPost]
        public async Task<IActionResult> CreateTattooSession(
            CreateTattooSessionDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.CreateTattooSessionAsync(dto, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Tattoo session created successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Client)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTattooSession(
            int id,
            UpdateTattooSessionDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.UpdateTattooSessionAsync(id, dto, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Tattoo session updated successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTattooSession(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.DeleteTattooSessionAsync(id, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Tattoo session deleted successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
        [HttpPut("add-more-sessions/{tattooRequestId}")]
        public async Task<IActionResult> AddMoreSessions(
            int tattooRequestId,
            AddAdditionalSessionsDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.AddMoreSessionsAsync(
                tattooRequestId,
                dto,
                userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("More sessions added successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
        [HttpPut("complete-tattoo/{tattooRequestId}")]
        public async Task<IActionResult> CompleteTattoo(int tattooRequestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.CompleteTattooAsync(
                tattooRequestId,
                userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Tattoo completed successfully.");
        }

        [HttpPut("continue-tattoo/{tattooRequestId}")]
        public async Task<IActionResult> ContinueTattoo(int tattooRequestId)
        {
            var result = await service.ContinueTattooAsync(tattooRequestId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Tattoo continued successfully.");
        }
    }
}