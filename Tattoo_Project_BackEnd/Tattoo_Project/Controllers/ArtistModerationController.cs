using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs;
using Tattoo_Project.DTOs.AdminDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers;

[ApiController]
[Route("api/moderation")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class ArtistModerationController(IArtistModerationService service) : ControllerBase
{
    [Authorize(Roles = UserRoles.Client + "," + UserRoles.TattooArtist)]
    [HttpPost("artists/{artistId:int}/reports")]
    public async Task<IActionResult> ReportArtist(int artistId, CreateArtistReportDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await service.CreateReportAsync(userId, artistId, dto);
        return result.Success ? Ok("Report submitted for admin review.") : BadRequest(result.ErrorMessage);
    }

    [Authorize(Roles = UserRoles.TattooArtist)]
    [HttpGet("artist/status")]
    public async Task<IActionResult> GetMyStatus()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await service.GetMyStatusAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [Authorize(Roles = UserRoles.TattooArtist)]
    [HttpPost("artist/reinstatement/images")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> AddReinstatementImage(IFormFile image)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await service.AddReinstatementImageAsync(userId, image);
        return result.Success ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [Authorize(Roles = UserRoles.TattooArtist)]
    [HttpDelete("artist/reinstatement/images/{imageId:int}")]
    public async Task<IActionResult> DeleteReinstatementImage(int imageId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await service.DeleteReinstatementImageAsync(userId, imageId);
        return result.Success ? Ok("Moderation image deleted.") : BadRequest(result.ErrorMessage);
    }

    [Authorize(Roles = UserRoles.TattooArtist)]
    [HttpPost("artist/reinstatement/submit")]
    public async Task<IActionResult> SubmitReinstatement()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await service.SubmitReinstatementAsync(userId);
        return result.Success ? Ok("Your updated portfolio was submitted for admin review.") : BadRequest(result.ErrorMessage);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("admin/reports")]
    public async Task<IActionResult> GetReports()
    {
        var result = await service.GetReportsAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("admin/submissions")]
    public async Task<IActionResult> GetSubmissions()
    {
        var result = await service.GetSubmissionsAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("admin/reports/{reportId:int}/review")]
    public async Task<IActionResult> ReviewReport(int reportId, ModerateArtistReportRequest request)
    {
        var adminId = GetUserId();
        if (adminId == null) return Unauthorized();
        var result = await service.ReviewReportAsync(reportId, adminId, request);
        return result.Success ? Ok("Report review completed.") : BadRequest(result.ErrorMessage);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("admin/submissions/{submissionId:int}/review")]
    public async Task<IActionResult> ReviewSubmission(int submissionId, ModerateArtistSubmissionRequest request)
    {
        var adminId = GetUserId();
        if (adminId == null) return Unauthorized();
        var result = await service.ReviewSubmissionAsync(submissionId, adminId, request);
        return result.Success ? Ok("Moderation submission review completed.") : BadRequest(result.ErrorMessage);
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
