using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TattooSessionController(ITattooSessionService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<GetTattooSessionDto>>> GetAllTattooSessionsAsync()
        {
            var tattooSessions = await service.GetAllTattooSessionsAsync();
            if (tattooSessions == null || !tattooSessions.Any())
            {
                return NotFound("No tattoo sessions yet!");
            }

            return Ok(tattooSessions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GetTattooSessionDto>> GetATattooSessionByIdAsync(int id)
        {
            var tattooSession = await service.GetTattooSessionByIdAsync(id);
            if (tattooSession == null)
            {
                return NotFound($"Tattoo request with id {id} doesn't exist!");
            }

            return Ok(tattooSession);
        }

        [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = UserRoles.Client)]
        [HttpPost]
        public async Task<IActionResult> CreateTattooSession(CreateTattooSessionDto dto)
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTattooSessionAsync(int id, UpdateTattooSessionDto dto)
        {
            var isUpdated = await service.UpdateTattooSessionAsync(id, dto);

            return Ok(isUpdated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTattooSessionAsync(int id)
        {
            var isDeleted = await service.DeleteTattooSessionAsync(id);

            return Ok(isDeleted);
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

        [HttpPut("continue-tattoo/{tattooRequestId}")]
        public async Task<IActionResult> CountinueTattooAsync(int tattooRequestId)
        {
            var result = await service.ContinueTattooAsync(tattooRequestId);

            if (!result)
            {
                return BadRequest();
            }

            return Ok("Tattoo countinue.");
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
    }
}
