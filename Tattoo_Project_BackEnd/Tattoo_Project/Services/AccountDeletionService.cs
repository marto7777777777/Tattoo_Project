using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Tattoo_Project.Data;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;
namespace Tattoo_Project.Services;
public class AccountDeletionService(TattooDbContext db,UserManager<ApplicationUser> users,IAdminService admin,IConfiguration config):IAccountDeletionService
{
 public async Task<ResultService> DeleteAsync(string userId,string password,string confirmation)
 {
  if(confirmation!="DELETE")return ResultService.Fail("Type DELETE to confirm account deletion.");
  var user=await users.FindByIdAsync(userId);if(user==null)return ResultService.Fail("User was not found.");
  if(await users.IsInRoleAsync(user,UserRoles.Admin))return ResultService.Fail("Admin accounts require manual deletion review.");
  if(!await users.CheckPasswordAsync(user,password))return ResultService.Fail("Password is incorrect.");
  var subscription=await db.ArtistSubscriptions.AsNoTracking().FirstOrDefaultAsync(x=>x.TattooArtist.UserId==userId);
  if(subscription!=null&&!string.IsNullOrWhiteSpace(config["Stripe:SecretKey"]))
  {
   StripeConfiguration.ApiKey=config["Stripe:SecretKey"];
   try
   {
    if(!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))await new SubscriptionService().CancelAsync(subscription.StripeSubscriptionId);
    if(!string.IsNullOrWhiteSpace(subscription.StripeCustomerId))await new CustomerService().DeleteAsync(subscription.StripeCustomerId);
   }
   catch(StripeException){return ResultService.Fail("Stripe billing data could not be closed. Account deletion was not performed.");}
  }
  var result=await admin.DeleteUserAsync(userId,"self-service-account-deletion");if(!result.Success)return result;
  db.AccountDeletionAudits.Add(new(){DeletedAt=DateTime.UtcNow,RetentionNote="Deletion audit retained for legal and security accountability; no user identifier is stored in this audit record."});await db.SaveChangesAsync();return ResultService.Ok();
 }
}
