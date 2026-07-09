using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ArtistUnavailableDateDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class ArtistUnavailableDateService : IArtistUnavailableDateService
    {
        private readonly TattooDbContext context;

        public ArtistUnavailableDateService(TattooDbContext context)
        {
            this.context = context;
        }

        public async Task<ResultService> CreateUnavailableDateAsync(CreateArtistUnavailableDateDto dto, string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile not found.");
            }

            if (dto.StartDateTime >= dto.EndDateTime)
            {
                return ResultService.Fail("Start date must be before end date.");
            }

            if (dto.StartDateTime < DateTime.UtcNow)
            {
                return ResultService.Fail("You cannot mark past time as unavailable.");
            }

            var hasExistingUnavailablePeriod = await context.ArtistUnavailableDates
                .AnyAsync(u =>
                    u.TattooArtistId == tattooArtist.Id &&
                    dto.StartDateTime < u.EndDateTime &&
                    dto.EndDateTime > u.StartDateTime);

            if (hasExistingUnavailablePeriod)
            {
                return ResultService.Fail("You already marked this period as unavailable.");
            }

            var hasConsultationConflict = await context.Consultations
                .AnyAsync(c =>
                    c.TattooRequest.TattooArtistId == tattooArtist.Id &&
                    dto.StartDateTime < c.EndTime &&
                    dto.EndDateTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return ResultService.Fail("You already have a consultation in this period. Please cancel it before marking this time as unavailable.");
            }

            var hasTattooSessionConflict = await context.TattooSessions
                .AnyAsync(s =>
                    s.TattooRequest.TattooArtistId == tattooArtist.Id &&
                    dto.StartDateTime < s.EndTime &&
                    dto.EndDateTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return ResultService.Fail("You already have a tattoo session in this period. Please cancel it before marking this time as unavailable.");
            }

            var unavailableDate = new ArtistUnavailableDate
            {
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                TattooArtistId = tattooArtist.Id
            };

            await context.ArtistUnavailableDates.AddAsync(unavailableDate);
            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService<ICollection<GetArtistUnavailableDateDto>>> GetMyUnavailableDatesAsync(string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService<ICollection<GetArtistUnavailableDateDto>>.Fail("Tattoo artist profile not found.");
            }

            var unavailableDates = await context.ArtistUnavailableDates
                .Where(u => u.TattooArtistId == tattooArtist.Id)
                .OrderBy(u => u.StartDateTime)
                .Select(u => new GetArtistUnavailableDateDto
                {
                    Id = u.Id,
                    StartDateTime = u.StartDateTime,
                    EndDateTime = u.EndDateTime
                })
                .ToListAsync();

            return ResultService<ICollection<GetArtistUnavailableDateDto>>.Ok(unavailableDates);
        }

        public async Task<ResultService> DeleteUnavailableDateAsync(int id, string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile not found.");
            }

            var unavailableDate = await context.ArtistUnavailableDates
                .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    u.TattooArtistId == tattooArtist.Id);

            if (unavailableDate == null)
            {
                return ResultService.Fail("Unavailable period not found.");
            }

            context.ArtistUnavailableDates.Remove(unavailableDate);
            await context.SaveChangesAsync();

            return ResultService.Ok();
        }
    }
}