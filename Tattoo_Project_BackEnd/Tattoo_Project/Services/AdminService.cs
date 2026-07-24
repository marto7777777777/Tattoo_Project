using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.AdminDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public class AdminService(
    TattooDbContext context,
    UserManager<ApplicationUser> userManager) : IAdminService
{
    public async Task<ResultService<AdminOverviewDto>> GetOverviewAsync()
    {
        var dto = new AdminOverviewDto
        {
            Users = await userManager.Users.CountAsync(),
            Clients = await context.Clients.CountAsync(),
            Artists = await context.TattooArtists.CountAsync(),
            TattooRequests = await context.TattooRequests.CountAsync(),
            AiProjects = await context.AiTattooProjects.CountAsync()
        };

        return ResultService<AdminOverviewDto>.Ok(dto);
    }

    public async Task<ResultService<ICollection<AdminUserDto>>> GetUsersAsync()
    {
        var users = await userManager.Users
            .OrderBy(x => x.Email)
            .ToListAsync();

        var clientProfiles = await context.Clients
            .Select(x => new { x.Id, x.UserId })
            .ToDictionaryAsync(x => x.UserId, x => x.Id);

        var artistProfiles = await context.TattooArtists
            .Select(x => new { x.Id, x.UserId })
            .ToDictionaryAsync(x => x.UserId, x => x.Id);

        var clientRequestCounts = await context.TattooRequests
            .GroupBy(x => x.Client.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var artistRequestCounts = await context.TattooRequests
            .GroupBy(x => x.TattooArtist.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var aiCounts = await context.AiTattooProjects
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var result = new List<AdminUserDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            clientRequestCounts.TryGetValue(user.Id, out var clientCount);
            artistRequestCounts.TryGetValue(user.Id, out var artistCount);
            aiCounts.TryGetValue(user.Id, out var aiCount);

            result.Add(new AdminUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList(),
                ClientProfileId = clientProfiles.TryGetValue(user.Id, out var clientProfileId) ? clientProfileId : null,
                ArtistProfileId = artistProfiles.TryGetValue(user.Id, out var artistProfileId) ? artistProfileId : null,
                TattooRequestCount = clientCount + artistCount,
                AiProjectCount = aiCount
            });
        }

        return ResultService<ICollection<AdminUserDto>>.Ok(result);
    }

    public async Task<ResultService<ICollection<AdminTattooRequestDto>>> GetTattooRequestsAsync()
    {
        var requests = await context.TattooRequests
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.TattooArtist)
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new AdminTattooRequestDto
            {
                Id = x.Id,
                ClientName = x.Client.FirstName + " " + x.Client.LastName,
                ArtistName = x.TattooArtist.FirstName + " " + x.TattooArtist.LastName,
                Placement = x.Placement,
                TattooStyle = x.TattooStyle,
                Status = x.Status.ToString(),
                CreatedOn = x.CreatedOn
            })
            .ToListAsync();

        return ResultService<ICollection<AdminTattooRequestDto>>.Ok(requests);
    }


    public async Task<ResultService<ICollection<AdminAiProjectDto>>> GetAiProjectsAsync()
    {
        var projects = await context.AiTattooProjects
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Versions)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new AdminAiProjectDto
            {
                Id = x.Id,
                UserEmail = x.User.Email ?? string.Empty,
                Title = x.Title,
                TattooStyle = x.TattooStyle,
                Placement = x.Placement,
                VersionCount = x.Versions.Count,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return ResultService<ICollection<AdminAiProjectDto>>.Ok(projects);
    }

    public async Task<ResultService> DeleteUserAsync(string userId, string currentAdminUserId)
    {
        if (userId == currentAdminUserId)
            return ResultService.Fail("You cannot delete the admin account you are currently using.");

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return ResultService.Fail("User was not found.");

        if (await userManager.IsInRoleAsync(user, UserRoles.Admin))
            return ResultService.Fail("Admin accounts cannot be deleted from the admin panel.");

        var client = await context.Clients.FirstOrDefaultAsync(x => x.UserId == userId);
        if (client != null)
        {
            var result = await DeleteClientProfileInternalAsync(client.Id, removeRole: false);
            if (!result.Success) return result;
        }

        var artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.UserId == userId);
        if (artist != null)
        {
            var result = await DeleteArtistProfileInternalAsync(artist.Id, removeRole: false);
            if (!result.Success) return result;
        }

        await context.EmailVerificationCodes
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync();

        var aiProjectIds = await context.AiTattooProjects
            .Where(x => x.UserId == userId)
            .Select(x => x.Id)
            .ToListAsync();

        foreach (var aiProjectId in aiProjectIds)
            await DeleteAiProjectGraphAsync(aiProjectId);

        var identityResult = await userManager.DeleteAsync(user);
        if (!identityResult.Succeeded)
            return ResultService.Fail(string.Join(" ", identityResult.Errors.Select(x => x.Description)));

        return ResultService.Ok();
    }

    public Task<ResultService> DeleteClientProfileAsync(int clientId)
        => DeleteClientProfileInternalAsync(clientId, removeRole: true);

    public Task<ResultService> DeleteArtistProfileAsync(int artistId)
        => DeleteArtistProfileInternalAsync(artistId, removeRole: true);

    public async Task<ResultService> DeleteTattooRequestAsync(int tattooRequestId)
    {
        var exists = await context.TattooRequests.AnyAsync(x => x.Id == tattooRequestId);
        if (!exists) return ResultService.Fail("Tattoo request was not found.");

        await DeleteTattooRequestGraphAsync(tattooRequestId);
        return ResultService.Ok();
    }

    public async Task<ResultService> DeleteAiProjectAsync(int projectId)
    {
        var exists = await context.AiTattooProjects.AnyAsync(x => x.Id == projectId);
        if (!exists) return ResultService.Fail("AI tattoo project was not found.");

        await DeleteAiProjectGraphAsync(projectId);
        return ResultService.Ok();
    }


    public async Task<ResultService> SetArtistVerifiedAsync(int artistId, bool isVerified)
    {
        var artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.Id == artistId);
        if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");

        artist.IsVerified = isVerified;
        await context.SaveChangesAsync();
        return ResultService.Ok();
    }

    private async Task<ResultService> DeleteClientProfileInternalAsync(int clientId, bool removeRole)
    {
        var client = await context.Clients.FirstOrDefaultAsync(x => x.Id == clientId);
        if (client == null) return ResultService.Fail("Client profile was not found.");

        var requestIds = await context.TattooRequests
            .Where(x => x.ClientId == clientId)
            .Select(x => x.Id)
            .ToListAsync();

        foreach (var requestId in requestIds)
            await DeleteTattooRequestGraphAsync(requestId);

        await context.ArtistReviews.Where(x => x.ClientId == clientId).ExecuteDeleteAsync();
        await context.ClientFavoriteArtists.Where(x => x.ClientId == clientId).ExecuteDeleteAsync();

        context.Clients.Remove(client);
        await context.SaveChangesAsync();

        if (removeRole)
        {
            var user = await userManager.FindByIdAsync(client.UserId);
            if (user != null && await userManager.IsInRoleAsync(user, UserRoles.Client))
                await userManager.RemoveFromRoleAsync(user, UserRoles.Client);
        }

        return ResultService.Ok();
    }

    private async Task<ResultService> DeleteArtistProfileInternalAsync(int artistId, bool removeRole)
    {
        var artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.Id == artistId);
        if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");

        var ownedStudio = await context.Studios.FirstOrDefaultAsync(s => s.OwnerArtistId == artistId);
        if (ownedStudio != null)
        {
            var nextOwner = await context.TattooArtists
                .Where(a => a.StudioId == ownedStudio.Id && a.Id != artistId)
                .OrderBy(a => a.JoinedStudioOn ?? DateTime.MaxValue)
                .ThenBy(a => a.Id)
                .FirstOrDefaultAsync();

            if (nextOwner != null)
            {
                ownedStudio.OwnerArtistId = nextOwner.Id;
            }
            else
            {
                ownedStudio.OwnerArtistId = null;
                artist.StudioId = null;
                await context.SaveChangesAsync();
                context.Studios.Remove(ownedStudio);
                await context.SaveChangesAsync();
            }
        }

        var requestIds = await context.TattooRequests
            .Where(x => x.TattooArtistId == artistId)
            .Select(x => x.Id)
            .ToListAsync();

        foreach (var requestId in requestIds)
            await DeleteTattooRequestGraphAsync(requestId);

        await context.ArtistReviews.Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();
        await context.ClientFavoriteArtists.Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();
        await context.ArtistUnavailableDates.Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();
        await context.Schedules.Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();
        await context.Set<ArtistRequirement>().Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();
        await context.Set<PortfolioImage>().Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();

        context.TattooArtists.Remove(artist);
        await context.SaveChangesAsync();

        if (removeRole)
        {
            var user = await userManager.FindByIdAsync(artist.UserId);
            if (user != null && await userManager.IsInRoleAsync(user, UserRoles.TattooArtist))
                await userManager.RemoveFromRoleAsync(user, UserRoles.TattooArtist);
        }

        return ResultService.Ok();
    }


    private async Task DeleteAiProjectGraphAsync(int projectId)
    {
        await context.AiTattooVersions
            .Where(x => x.AiTattooProjectId == projectId && x.ParentVersionId != null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ParentVersionId, (int?)null));

        await context.AiProjectPayments.Where(x => x.AiTattooProjectId == projectId).ExecuteDeleteAsync();
        await context.AiTattooVersions.Where(x => x.AiTattooProjectId == projectId).ExecuteDeleteAsync();
        await context.AiTattooProjects.Where(x => x.Id == projectId).ExecuteDeleteAsync();
    }

    private async Task DeleteTattooRequestGraphAsync(int tattooRequestId)
    {
        await context.ArtistReviews.Where(x => x.TattooRequestId == tattooRequestId).ExecuteDeleteAsync();
        await context.TattooSessions.Where(x => x.TattooRequestId == tattooRequestId).ExecuteDeleteAsync();
        await context.Consultations.Where(x => x.TattooRequestId == tattooRequestId).ExecuteDeleteAsync();
        await context.ArtistResponses.Where(x => x.TattooRequestId == tattooRequestId).ExecuteDeleteAsync();
        await context.TattooReferenceImages.Where(x => x.TattooRequestId == tattooRequestId).ExecuteDeleteAsync();
        await context.TattooRequests.Where(x => x.Id == tattooRequestId).ExecuteDeleteAsync();
    }
}
