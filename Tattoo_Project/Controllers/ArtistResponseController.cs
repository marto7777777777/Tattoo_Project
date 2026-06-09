using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.ArtistResponseDTOs;
using Tattoo_Project.Services;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArtistResponseController(IArtistResponseService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<GetArtistResponseDto>>> GetAllArtistResponsesAsync()
        {
            var artistsResponses = await service.GetAllArtistResponsesAsync();
            if (artistsResponses == null || !artistsResponses.Any())
            {
                return NotFound($"No artist responses yet!");
            }
            return Ok(artistsResponses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GetArtistResponseDto>> GetArtistResponseByIdAsync(int id)
        {
            var artist = await service.GetArtistResponseByIdAsync(id);
            if (artist == null)
            {
                return NotFound($"Artist response with id {id} doesn't exist!");
            }
            return Ok(artist);
        }

        [HttpPost]
        public async Task<IActionResult> CreateArtistResponse(CreateArtistResponseDto dto)
        {
            var isCreated = await service.CreateArtistResponseAsync(dto);
            return Ok(isCreated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArtistResponse(int id)
        {
            var isDeleted = await service.DeleteArtistResponseAsync(id);
            return Ok(isDeleted);
        }
    }
}
