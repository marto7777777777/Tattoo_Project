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
            var tattooSessions = await service.GetAllTattooSessionsAsync();

            return Ok(tattooSessions);
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

            var tattooSession = await service.GetTattooSessionByIdAsync(
                id,
                userId,
                User.IsInRole(UserRoles.Admin),
                User.IsInRole(UserRoles.Client),
                User.IsInRole(UserRoles.TattooArtist));

            if (tattooSession == null)
            {
                return NotFound("Tattoo session not found.");
            }

            return Ok(tattooSession);
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

            var isCreated = await service.CreateTattooSessionAsync(dto, userId);

            if (!isCreated)
            {
                return BadRequest("Tattoo session could not be created.");
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

            var isUpdated = await service.UpdateTattooSessionAsync(
                id,
                dto,
                userId);

            if (!isUpdated)
            {
                return BadRequest("Tattoo session could not be updated.");
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

            var isDeleted = await service.DeleteTattooSessionAsync(id, userId);

            if (!isDeleted)
            {
                return BadRequest("Tattoo session could not be deleted.");
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

            var isAdded = await service.AddMoreSessionsAsync(
                tattooRequestId,
                dto,
                userId);

            if (!isAdded)
            {
                return BadRequest("More sessions could not be added.");
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

            var isCompleted = await service.CompleteTattooAsync(
                tattooRequestId,
                userId);

            if (!isCompleted)
            {
                return BadRequest("Tattoo could not be completed.");
            }

            return Ok("Tattoo completed successfully.");
        }

        // Optional / future feature.
        // Оставяме го без Identity засега, защото не е част от активния workflow.
        [HttpPut("continue-tattoo/{tattooRequestId}")]
        public async Task<IActionResult> ContinueTattoo(int tattooRequestId)
        {
            var isContinued = await service.ContinueTattooAsync(tattooRequestId);

            if (!isContinued)
            {
                return BadRequest("Tattoo could not be continued.");
            }

            return Ok("Tattoo continued successfully.");
        }
    }
}