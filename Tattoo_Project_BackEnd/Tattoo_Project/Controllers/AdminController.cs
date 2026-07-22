using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Admin)]
public class AdminController(IAdminService service) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var result = await service.GetOverviewAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await service.GetUsersAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [HttpGet("tattoo-requests")]
    public async Task<IActionResult> GetTattooRequests()
    {
        var result = await service.GetTattooRequestsAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [HttpGet("ai-projects")]
    public async Task<IActionResult> GetAiProjects()
    {
        var result = await service.GetAiProjectsAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentAdminId)) return Unauthorized();

        var result = await service.DeleteUserAsync(userId, currentAdminId);
        return result.Success ? Ok("User and related data deleted successfully.") : BadRequest(result.ErrorMessage);
    }

    [HttpDelete("client-profiles/{clientId:int}")]
    public async Task<IActionResult> DeleteClientProfile(int clientId)
    {
        var result = await service.DeleteClientProfileAsync(clientId);
        return result.Success ? Ok("Client profile deleted successfully.") : BadRequest(result.ErrorMessage);
    }

    [HttpDelete("artist-profiles/{artistId:int}")]
    public async Task<IActionResult> DeleteArtistProfile(int artistId)
    {
        var result = await service.DeleteArtistProfileAsync(artistId);
        return result.Success ? Ok("Artist profile deleted successfully.") : BadRequest(result.ErrorMessage);
    }

    [HttpDelete("tattoo-requests/{tattooRequestId:int}")]
    public async Task<IActionResult> DeleteTattooRequest(int tattooRequestId)
    {
        var result = await service.DeleteTattooRequestAsync(tattooRequestId);
        return result.Success ? Ok("Tattoo request deleted successfully.") : BadRequest(result.ErrorMessage);
    }

    [HttpDelete("ai-projects/{projectId:int}")]
    public async Task<IActionResult> DeleteAiProject(int projectId)
    {
        var result = await service.DeleteAiProjectAsync(projectId);
        return result.Success ? Ok("AI project deleted successfully.") : BadRequest(result.ErrorMessage);
    }

    [HttpPatch("artist-profiles/{artistId:int}/verified")]
    public async Task<IActionResult> SetArtistVerified(int artistId, [FromBody] SetArtistVerifiedRequest request)
    {
        var result = await service.SetArtistVerifiedAsync(artistId, request.IsVerified);
        return result.Success ? Ok("Artist verification updated successfully.") : BadRequest(result.ErrorMessage);
    }

    public sealed class SetArtistVerifiedRequest
    {
        public bool IsVerified { get; set; }
    }
}
