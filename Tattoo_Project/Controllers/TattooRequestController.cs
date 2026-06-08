using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.TattooRequestDTOs;
using Tattoo_Project.Services;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TattooRequestController(ITattooRequestService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<GetTattooRequestDto>>> GetAllTattooRequestsAsync()
        {
            var tattooRequests = await service.GetAllTattooRequestsAsync();
            if (tattooRequests is null || !tattooRequests.Any())
            {
                return NotFound("No tattoo requests yet!");
            }
            return Ok(tattooRequests);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CreateTattooRequestDto>> GetTattooRequestById(int id)
        {
            var tattooRequest = service.GetTattooRequestByIdAsync(id);
            if (tattooRequest is null)
            {
               return NotFound($"Tattoo request with id {id} doesn't exist!");
            }
            return Ok(tattooRequest);
        }
    }
}
