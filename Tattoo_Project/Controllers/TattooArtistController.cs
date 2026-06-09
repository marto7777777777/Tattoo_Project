using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.TattooArtistDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TattooArtistController(ITattooArtistService service) : ControllerBase
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

        [HttpPost]
        public async Task<IActionResult> CreateArtist(CreateTattooArtistDto dto)
        {
            var id = await service.CreateArtist(dto);
            return Ok(new { id });
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
