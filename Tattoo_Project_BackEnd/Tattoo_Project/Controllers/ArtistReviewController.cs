using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.ArtistReviewDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArtistReviewController : ControllerBase
    {
        private readonly IArtistReviewService artistReviewService;

        public ArtistReviewController(IArtistReviewService artistReviewService)
        {
            this.artistReviewService = artistReviewService;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Client)]
        public async Task<IActionResult> CreateArtistReview(CreateArtistReviewDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await artistReviewService.CreateArtistReviewAsync(dto, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Review created successfully.");
        }

        [HttpGet("artist/{tattooArtistId}")]
        public async Task<IActionResult> GetArtistReviews(int tattooArtistId)
        {
            var result = await artistReviewService.GetArtistReviewsByArtistIdAsync(tattooArtistId);

            if (!result.Success)
            {
                return NotFound(result.ErrorMessage);
            }

            return Ok(result.Data);
        }
    }
}