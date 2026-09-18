using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.Data;
using Tattoo_Project.Models;

namespace Tattoo_Project.Controllers;

[ApiController]
[Route("api/legal")]
public class LegalController(IConfiguration configuration, UserManager<ApplicationUser> userManager, TattooDbContext db, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("versions")]
    [AllowAnonymous]
    public IActionResult GetVersions() => Ok(new
    {
        TermsVersion = configuration["Legal:TermsVersion"],
        PrivacyVersion = configuration["Legal:PrivacyVersion"],
        TermsUrl = configuration["Legal:TermsUrl"] ?? "/legal/terms",
        PrivacyUrl = configuration["Legal:PrivacyUrl"] ?? "/legal/privacy"
    });

    [HttpGet("consent-status")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetConsentStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); if (userId == null) return Unauthorized();
        var user = await userManager.FindByIdAsync(userId); if (user == null) return Unauthorized();
        var terms = configuration["Legal:TermsVersion"] ?? string.Empty;
        var privacy = configuration["Legal:PrivacyVersion"] ?? string.Empty;
        return Ok(new { RequiresReconsent = user.TermsVersion != terms || user.PrivacyVersion != privacy, TermsVersion = terms, PrivacyVersion = privacy });
    }

    [HttpPost("consent")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> AcceptCurrentVersions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); if (userId == null) return Unauthorized();
        var user = await userManager.FindByIdAsync(userId); if (user == null) return Unauthorized();
        var terms = configuration["Legal:TermsVersion"]; var privacy = configuration["Legal:PrivacyVersion"];
        if (string.IsNullOrWhiteSpace(terms) || string.IsNullOrWhiteSpace(privacy)) return StatusCode(500, "Legal versions are not configured.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        user.TermsVersion = terms; user.PrivacyVersion = privacy; user.TermsAcceptedAt = now; user.PrivacyAcceptedAt = now;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(string.Join(" ", result.Errors.Select(x => x.Description)));
        db.Set<LegalConsentAudit>().Add(new LegalConsentAudit { UserId = userId, TermsVersion = terms, PrivacyVersion = privacy, AcceptedAt = now });
        await db.SaveChangesAsync();
        return Ok(new { TermsVersion = terms, PrivacyVersion = privacy, AcceptedAt = now });
    }
}
