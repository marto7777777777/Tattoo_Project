using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.TattooRequestDTOs;
using Tattoo_Project.Services.Interfaces;

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
            var tattooRequest = await service.GetTattooRequestByIdAsync(id);
            if (tattooRequest is null)
            {
               return NotFound($"Tattoo request with id {id} doesn't exist!");
            }
            return Ok(tattooRequest);
        }

        [HttpPost]
        public async Task<ActionResult<bool>> CreateTattooRequest(CreateTattooRequestDto dto)
        {
            var isCreated = await service.CreateTattooRequest(dto);
            return Ok(isCreated);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<bool>> UpdateTattooRequest(int id, UpdateTattooRequestDto dto)
        {
            var isUpdated = await service.UpdateTattooRequest(id, dto);

            return Ok(isUpdated);
            
        }

    }
}
