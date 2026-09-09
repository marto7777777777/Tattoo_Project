using Microsoft.AspNetCore.Authorization;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
namespace Tattoo_Project.Authorization;
public class ActiveArtistSubscriptionRequirement:IAuthorizationRequirement;
public class ActiveArtistSubscriptionHandler(IArtistSubscriptionService subscriptions):AuthorizationHandler<ActiveArtistSubscriptionRequirement>
{
 protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,ActiveArtistSubscriptionRequirement requirement)
 {
  if(context.User.IsInRole(UserRoles.Admin)){context.Succeed(requirement);return;}
  var userId=context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
  if(userId!=null&&await subscriptions.HasAccessAsync(userId))context.Succeed(requirement);
 }
}
