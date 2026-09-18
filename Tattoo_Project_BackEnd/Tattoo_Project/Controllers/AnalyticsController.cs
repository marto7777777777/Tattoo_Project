using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Authentication.JwtBearer;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using System.Security.Claims;using Tattoo_Project.Data;using Tattoo_Project.DTOs.AnalyticsDTOs;using Tattoo_Project.Models;
namespace Tattoo_Project.Controllers;
[ApiController,Route("api/analytics"),Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme,Roles=UserRoles.TattooArtist)]
public class AnalyticsController(TattooDbContext db, TimeProvider timeProvider):ControllerBase
{
 [HttpPost("pending-events/consume")]
 public async Task<IActionResult> Consume()
 {
  var uid=User.FindFirstValue(ClaimTypes.NameIdentifier);var artistId=await db.TattooArtists.Where(x=>x.UserId==uid).Select(x=>(int?)x.Id).FirstOrDefaultAsync();if(artistId==null)return Ok(Array.Empty<AnalyticsEventDto>());
  await using var transaction=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
  var items=await db.AnalyticsOutboxEvents.Where(x=>x.TattooArtistId==artistId&&x.DispatchedAt==null).OrderBy(x=>x.Id).Take(100).ToListAsync();var result=items.Select(x=>new AnalyticsEventDto{Name=x.Name,CompletedProjectCount=x.CompletedProjectCount}).ToList();foreach(var item in items)item.DispatchedAt=timeProvider.GetUtcNow().UtcDateTime;await db.SaveChangesAsync();await transaction.CommitAsync();return Ok(result);
 }
}
