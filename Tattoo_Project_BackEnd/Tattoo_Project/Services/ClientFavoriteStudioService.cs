using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.StudioDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class ClientFavoriteStudioService(TattooDbContext context, IPrivateMediaUrlService mediaUrls, TimeProvider timeProvider) : IClientFavoriteStudioService
    {
        public async Task<ResultService> AddAsync(int studioId, string userId)
        {
            var client = await context.Clients.FirstOrDefaultAsync(x => x.UserId == userId);
            if (client == null) return ResultService.Fail("Client profile not found.");
            if (!await context.Studios.AnyAsync(x => x.Id == studioId && x.Artists.Any()))
                return ResultService.Fail("Studio not found.");
            if (await context.ClientFavoriteStudios.AnyAsync(x => x.ClientId == client.Id && x.StudioId == studioId))
                return ResultService.Fail("This studio is already in your favorites.");

            context.ClientFavoriteStudios.Add(new ClientFavoriteStudio
            {
                ClientId = client.Id,
                StudioId = studioId,
                CreatedOn = timeProvider.GetUtcNow().UtcDateTime
            });
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> RemoveAsync(int studioId, string userId)
        {
            var favorite = await context.ClientFavoriteStudios
                .FirstOrDefaultAsync(x => x.Client.UserId == userId && x.StudioId == studioId);
            if (favorite == null) return ResultService.Fail("This studio is not in your favorites.");

            context.ClientFavoriteStudios.Remove(favorite);
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService<ICollection<StudioDto>>> GetMineAsync(string userId)
        {
            var clientExists = await context.Clients.AnyAsync(x => x.UserId == userId);
            if (!clientExists)
                return ResultService<ICollection<StudioDto>>.Fail("Client profile not found.");

            var favorites = await context.ClientFavoriteStudios
                .AsNoTracking()
                .Where(x => x.Client.UserId == userId && x.Studio.Artists.Any())
                .Include(x => x.Studio)
                    .ThenInclude(x => x.Artists)
                        .ThenInclude(x => x.User)
                .Include(x => x.Studio)
                    .ThenInclude(x => x.Artists)
                        .ThenInclude(x => x.Reviews)
                .Include(x => x.Studio)
                    .ThenInclude(x => x.Artists)
                        .ThenInclude(x => x.PortfolioImages)
                .Include(x => x.Studio)
                    .ThenInclude(x => x.Artists)
                        .ThenInclude(x => x.SpecialtyStyles)
                .Include(x => x.Studio)
                    .ThenInclude(x => x.Artists)
                        .ThenInclude(x => x.Subscription)
                .OrderByDescending(x => x.CreatedOn)
                .AsSplitQuery()
                .ToListAsync();

            var studios = favorites.Select(x => x.Studio).ToList();

            var now = timeProvider.GetUtcNow().UtcDateTime;

            return ResultService<ICollection<StudioDto>>.Ok(
                studios
                    .Select(s => StudioService.MapStudio(s, mediaUrls, now))
                    .ToList());
        }
    }
}
