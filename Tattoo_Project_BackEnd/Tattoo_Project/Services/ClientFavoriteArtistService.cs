using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.TattooArtistDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class ClientFavoriteArtistService : IClientFavoriteArtistService
    {
        private readonly TattooDbContext context;

        public ClientFavoriteArtistService(TattooDbContext context)
        {
            this.context = context;
        }

        public async Task<ResultService> AddFavoriteArtistAsync(int tattooArtistId, string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile not found.");
            }

            var artistExists = await context.TattooArtists
                .AnyAsync(a => a.Id == tattooArtistId && a.StudioId != null);

            if (!artistExists)
            {
                return ResultService.Fail("Tattoo artist not found.");
            }

            var alreadyFavorite = await context.ClientFavoriteArtists
                .AnyAsync(f =>
                    f.ClientId == client.Id &&
                    f.TattooArtistId == tattooArtistId);

            if (alreadyFavorite)
            {
                return ResultService.Fail("This artist is already in your favorites.");
            }

            var favorite = new ClientFavoriteArtist
            {
                ClientId = client.Id,
                TattooArtistId = tattooArtistId,
                CreatedOn = DateTime.UtcNow
            };

            await context.ClientFavoriteArtists.AddAsync(favorite);
            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> RemoveFavoriteArtistAsync(int tattooArtistId, string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile not found.");
            }

            var favorite = await context.ClientFavoriteArtists
                .FirstOrDefaultAsync(f =>
                    f.ClientId == client.Id &&
                    f.TattooArtistId == tattooArtistId);

            if (favorite == null)
            {
                return ResultService.Fail("This artist is not in your favorites.");
            }

            context.ClientFavoriteArtists.Remove(favorite);
            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService<ICollection<GetTattooArtistDto>>> GetMyFavoriteArtistsAsync(string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService<ICollection<GetTattooArtistDto>>.Fail("Client profile not found.");
            }

            var favoriteArtists = await context.ClientFavoriteArtists
                .Where(f => f.ClientId == client.Id && f.TattooArtist.StudioId != null)
                .Include(f => f.TattooArtist)
                    .ThenInclude(a => a.User)
                .Include(f => f.TattooArtist)
                    .ThenInclude(a => a.Studio)
                .Include(f => f.TattooArtist)
                    .ThenInclude(a => a.Reviews)
                .Include(f => f.TattooArtist)
                    .ThenInclude(a => a.Schedules)
                .Include(f => f.TattooArtist)
                    .ThenInclude(a => a.PortfolioImages)
                .Include(f => f.TattooArtist)
                    .ThenInclude(a => a.Requirements)
                .OrderByDescending(f => f.CreatedOn)
                .Select(f => f.TattooArtist)
                .ToListAsync();

            var result = favoriteArtists
                .Select(MapToGetTattooArtistDto)
                .ToList();

            return ResultService<ICollection<GetTattooArtistDto>>.Ok(result);
        }

        private static GetTattooArtistDto MapToGetTattooArtistDto(TattooArtist artist)
        {
            return new GetTattooArtistDto
            {
                Id = artist.Id,
                FirstName = artist.FirstName,
                LastName = artist.LastName,
                Email = artist.Email,
                ProfileImageUrl = artist.User?.ProfileImageUrl,

                StudioId = artist.StudioId,
                StudioName = artist.Studio?.Name ?? string.Empty,
                Description = artist.Description,
                StudioAddress = artist.Studio?.Address ?? string.Empty,
                StudioCity = artist.Studio?.City ?? string.Empty,
                StudioCountry = artist.Studio?.Country ?? string.Empty,
                StudioLatitude = artist.Studio?.Latitude,
                StudioLongitude = artist.Studio?.Longitude,
                PhoneNumber = artist.PhoneNumber,

                OffersOnlineConsultation = artist.OffersOnlineConsultation,
                RequiresDeposit = artist.RequiresDeposit,
                DepositAmount = artist.DepositAmount,

                IsVerified = artist.IsVerified,

                AverageRating = artist.Reviews.Any()
                    ? Math.Round(artist.Reviews.Average(r => r.Rating), 1)
                    : 0,

                ReviewCount = artist.Reviews.Count,

                ConsultationDurationMinutes = artist.ConsultationDurationMinutes,

                PortfolioImages = artist.PortfolioImages.Select(i => new TattooArtistPortfolioImageDto
                { 
                    Id = i.Id,
                    ImageUrl = i.ImageUrl
                }).ToList(),

                Requirements = artist.Requirements.Select(r => new TattooArtistRequirementsDto
                {
                    Id = r.Id,
                    Description = r.Description
                }).ToList(),

                Schedules = artist.Schedules.Select(s => new TattooArtistScheduleDto
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    ScheduleType = s.ScheduleType
                }).ToList()
            };
        }
    }
}