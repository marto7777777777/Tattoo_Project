using Microsoft.AspNetCore.Identity;
using Tattoo_Project.Models;

namespace Tattoo_Project.Services;

public static class AdminSeedService
{
    public static async Task EnsureAdminAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeed");

        var email = configuration["AdminSeed:Email"]?.Trim();
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Admin seed skipped. Configure AdminSeed:Email and AdminSeed:Password in user-secrets or environment variables.");
            return;
        }

        if (!await roleManager.RoleExistsAsync(UserRoles.Admin))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Could not create Admin role: " + string.Join(" ", roleResult.Errors.Select(x => x.Description)));
        }

        var user = await userManager.FindByEmailAsync(email);
        var userName = configuration["AdminSeed:UserName"]?.Trim();
        var firstName = configuration["AdminSeed:FirstName"]?.Trim();
        var lastName = configuration["AdminSeed:LastName"]?.Trim();

        if (user == null)
        {
            user = new ApplicationUser
            {
                Email = email,
                UserName = string.IsNullOrWhiteSpace(userName) ? "inkroute-admin" : userName,
                FirstName = string.IsNullOrWhiteSpace(firstName) ? "InkRoute" : firstName,
                LastName = string.IsNullOrWhiteSpace(lastName) ? "Admin" : lastName,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
                throw new InvalidOperationException("Could not create seeded admin: " + string.Join(" ", createResult.Errors.Select(x => x.Description)));
        }
        else
        {
            user.EmailConfirmed = true;
            if (!string.IsNullOrWhiteSpace(userName)) user.UserName = userName;
            if (!string.IsNullOrWhiteSpace(firstName)) user.FirstName = firstName;
            if (!string.IsNullOrWhiteSpace(lastName)) user.LastName = lastName;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException("Could not update seeded admin: " + string.Join(" ", updateResult.Errors.Select(x => x.Description)));

            if (!await userManager.CheckPasswordAsync(user, password))
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, resetToken, password);
                if (!resetResult.Succeeded)
                    throw new InvalidOperationException("Could not synchronize seeded admin password: " + string.Join(" ", resetResult.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, UserRoles.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, UserRoles.Admin);
            if (!addRoleResult.Succeeded)
                throw new InvalidOperationException("Could not assign Admin role: " + string.Join(" ", addRoleResult.Errors.Select(x => x.Description)));
        }

        logger.LogInformation("Seeded admin account is ready: {Email}", email);
    }
}
