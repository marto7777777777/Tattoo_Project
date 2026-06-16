using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.ArtistResponseDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class ArtistResponseController(IArtistResponseService service)
    : ControllerBase
{
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = UserRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAllArtistResponses()
    {
        var responses = await service.GetAllArtistResponsesAsync();

        return Ok(responses);
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = UserRoles.TattooArtist)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetArtistResponseById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        var response = await service.GetArtistResponseByIdAsync(id, userId);

        if (response == null)
        {
            return NotFound("Artist response not found.");
        }

        return Ok(response);
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = UserRoles.TattooArtist)]
    [HttpGet("my-responses")]
    public async Task<IActionResult> GetMyArtistResponses()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        var responses = await service.GetMyArtistResponsesAsync(userId);

        return Ok(responses);
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = UserRoles.TattooArtist)]
    [HttpPost]
    public async Task<IActionResult> CreateArtistResponse(CreateArtistResponseDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        var isCreated = await service.CreateArtistResponseAsync(dto, userId);

        if (!isCreated)
        {
            return BadRequest("Artist response could not be created.");
        }

        return Ok("Artist response created successfully.");
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = UserRoles.TattooArtist)]
    [HttpPut("reject-tattoo-request/{tattooRequestId}")]
    public async Task<IActionResult> RejectTattooRequest(int tattooRequestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        var isRejected = await service.RejectTattooRequestAsync(
            tattooRequestId,
            userId);

        if (!isRejected)
        {
            return BadRequest("Tattoo request could not be rejected.");
        }

        return Ok("Tattoo request rejected successfully.");
    }
}