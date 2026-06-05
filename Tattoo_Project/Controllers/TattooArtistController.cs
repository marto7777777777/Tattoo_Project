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
        public async Task<ActionResult<List<TattooArtistDto>>> GetArtists()
        {
            var tattooArtists = await service.GetAllArtistsAsync();
            if (tattooArtists is null || !tattooArtists.Any())
            {
               return NotFound("No artists yet!");
            }
            return Ok(tattooArtists);
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<TattooArtistDto>> GetArtistByIdAsync(int Id)
        {
            var tattooArtist = await service.GetTattooArtistByIdAsync(Id);
            if (tattooArtist is null)
            {
                return NotFound($"Artist with Id {Id} is not found!");
            }
            return Ok(tattooArtist);
        }
        
    }
}
