using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.StudioDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudioController(IStudioService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetStudios([FromQuery] string? query = null)
        {
            var result = await service.GetStudiosAsync(query);
            if (!result.Success) return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetStudio(int id)
        {
            var result = await service.GetStudioByIdAsync(id);
            if (!result.Success) return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("join-search")]
        public async Task<IActionResult> SearchForJoin([FromQuery] string? query)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await service.SearchOpenStudiosForJoinAsync(query, userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyStudio()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await service.GetMyStudioAsync(userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPost("mine/create")]
        public async Task<IActionResult> CreateMyStudio([FromBody] CreateStudioDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await service.CreateStudioForExistingArtistAsync(dto, userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);
            return Ok("Studio created successfully.");
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPost("{studioId:int}/join")]
        public async Task<IActionResult> RequestJoin(int studioId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await service.RequestJoinAsync(studioId, userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);
            return Ok("Join request sent successfully.");
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPost("join-requests/{requestId:int}/accept")]
        public async Task<IActionResult> AcceptJoinRequest(int requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await service.AcceptJoinRequestAsync(requestId, userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);
            return Ok("Artist added to the studio.");
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPost("join-requests/{requestId:int}/reject")]
        public async Task<IActionResult> RejectJoinRequest(int requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await service.RejectJoinRequestAsync(requestId, userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);
            return Ok("Join request rejected.");
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpDelete("members/{artistId:int}")]
        public async Task<IActionResult> RemoveMember(int artistId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await service.RemoveMemberAsync(artistId, userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);
            return Ok("Artist removed from the studio.");
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPatch("open-for-join-requests")]
        public async Task<IActionResult> SetOpenForJoinRequests(UpdateStudioOpenStateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await service.SetOpenForJoinRequestsAsync(dto.IsOpen, userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);
            return Ok(dto.IsOpen ? "Studio is now accepting join requests." : "Studio join requests are now closed.");
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPut("mine")]
        public async Task<IActionResult> UpdateMyStudio(UpdateStudioDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await service.UpdateStudioAsync(dto, userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);
            return Ok("Studio updated successfully.");
        }
    }
}
