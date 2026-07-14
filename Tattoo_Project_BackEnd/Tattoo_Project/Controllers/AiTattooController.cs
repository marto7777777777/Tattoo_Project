using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.AiTattooDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers;

[Route("api/ai-tattoos")]
[ApiController]
public class AiTattooController(IAiTattooService service, TattooDbContext context, IWebHostEnvironment environment) : ControllerBase
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Client + "," + UserRoles.TattooArtist)]
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var id=User.FindFirstValue(ClaimTypes.NameIdentifier); if(id==null)return Unauthorized();
        var r=await service.GetMyProjectsAsync(id); return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);
    }
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Client + "," + UserRoles.TattooArtist)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var uid=User.FindFirstValue(ClaimTypes.NameIdentifier); if(uid==null)return Unauthorized();
        var r=await service.GetProjectAsync(id,uid); return r.Success?Ok(r.Data):NotFound(r.ErrorMessage);
    }
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Client + "," + UserRoles.TattooArtist)]
    [HttpPost("paid-draft")]
    [RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> CreatePaidDraft([FromForm] CreateAiTattooProjectDto dto)
    {
        var uid=User.FindFirstValue(ClaimTypes.NameIdentifier); if(uid==null)return Unauthorized();
        var r=await service.CreatePaidDraftAsync(dto,uid); return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);
    }
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Client + "," + UserRoles.TattooArtist)]
    [HttpPost("{id:int}/generate")]
    public async Task<IActionResult> Generate(int id)
    {
        var uid=User.FindFirstValue(ClaimTypes.NameIdentifier); if(uid==null)return Unauthorized();
        var r=await service.GenerateInitialAsync(id,uid); return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Client + "," + UserRoles.TattooArtist)]
    [HttpPost]
    [RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> Create([FromForm] CreateAiTattooProjectDto dto)
    {
        var uid=User.FindFirstValue(ClaimTypes.NameIdentifier); if(uid==null)return Unauthorized();
        var r=await service.CreateProjectAsync(dto,uid); return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);
    }
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Client + "," + UserRoles.TattooArtist)]
    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id,[FromBody] EditAiTattooProjectDto dto)
    {
        var uid=User.FindFirstValue(ClaimTypes.NameIdentifier); if(uid==null)return Unauthorized();
        var r=await service.EditProjectAsync(id,dto,uid); return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Client + "," + UserRoles.TattooArtist)]
    [HttpGet("versions/{versionId:int}/download")]
    public async Task<IActionResult> DownloadVersion(int versionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var version = await context.AiTattooVersions
            .AsNoTracking()
            .Include(item => item.AiTattooProject)
            .FirstOrDefaultAsync(item =>
                item.Id == versionId &&
                item.AiTattooProject.UserId == userId);

        if (version is null)
        {
            return NotFound("AI tattoo version was not found.");
        }

        var fileName = Path.GetFileName(version.ImageUrl);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return NotFound("The generated image file was not found.");
        }

        var webRoot = environment.WebRootPath
            ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, "uploads", "ai-tattoos", fileName);

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound("The generated image file was not found.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "image/png"
        };

        var safeTitle = string.Join("-", version.AiTattooProject.Title
            .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .Trim();
        if (string.IsNullOrWhiteSpace(safeTitle))
        {
            safeTitle = "inkroute-tattoo";
        }

        var downloadName = $"{safeTitle}-v{version.VersionNumber}{extension}";
        return PhysicalFile(fullPath, contentType, downloadName, enableRangeProcessing: true);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = UserRoles.Client + "," + UserRoles.TattooArtist)]
    [HttpPost("{id:int}/checkout")]
    public async Task<IActionResult> Checkout(int id)
    {
        var uid=User.FindFirstValue(ClaimTypes.NameIdentifier); if(uid==null)return Unauthorized();
        var r=await service.CreateCheckoutAsync(id,uid); return r.Success?Ok(r.Data):BadRequest(r.ErrorMessage);
    }
    [AllowAnonymous]
    [HttpPost("stripe-webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        using var reader=new StreamReader(Request.Body); var payload=await reader.ReadToEndAsync(); var signature=Request.Headers["Stripe-Signature"].ToString();
        var r=await service.ProcessStripeWebhookAsync(payload,signature); return r.Success?Ok():BadRequest(r.ErrorMessage);
    }
}
