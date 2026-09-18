using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers;

[ApiController]
[Route("api/media")]
public sealed class PrivateMediaController(IPrivateMediaUrlService media) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("private")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Read([FromQuery] string token)
    {
        if (!media.TryResolveReadToken(token, out var fullPath, out var contentType)) return NotFound();
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(fullPath, contentType, enableRangeProcessing: false);
    }
    [AllowAnonymous]
    [HttpGet("public")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
    public IActionResult ReadPublic([FromQuery] string key)
    {
        if (!media.TryResolvePublicKey(key, out var fullPath, out var contentType)) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(fullPath, contentType, enableRangeProcessing: false);
    }

}
