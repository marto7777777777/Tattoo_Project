using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.TattooSessionDTOs;
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

        [HttpPost]
        public async Task<IActionResult> CreateTattooSessionAsync(CreateTattooSessionDto dto)
        {
            var isCreated = await service.CreateTattooSessionAsync(dto);

            return Ok(isCreated);
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
    }
}
