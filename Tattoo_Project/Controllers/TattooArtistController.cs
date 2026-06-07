using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services;

namespace Tattoo_Project.Controllers
{
    [ApiController]
    [Route("[controller]")]
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

        [HttpGet("{Id}")]
        public async Task<ActionResult<GetTattooArtistDto>> GetArtistByIdAsync(int Id)
        {
            var tattooArtist = await service.GetTattooArtistByIdAsync(Id);
            if (tattooArtist is null)
            {
                return NotFound($"Artist with Id {Id} is not found!");
            }
            return Ok(tattooArtist);
        }

        [HttpPost]
        public async Task<IActionResult> CreateArtist(CreateTattooArtistDto dto)
        {
            var id = await service.CreateArtist(dto);
            return Ok(new { id });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteArtist(int id)
        {
            var isDeleted = await service.DeleteArtist(id);
            if (isDeleted == false)
            {
                return NotFound($"Artist with id {id} already doesn't exist!");
            }
            return Ok(isDeleted);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateArtist(int id, UpdateArtistDto dto)
        {
            var isUpdated = await service.UpdateArtist(id, dto);
            return Ok(isUpdated);
        }
    }
}
