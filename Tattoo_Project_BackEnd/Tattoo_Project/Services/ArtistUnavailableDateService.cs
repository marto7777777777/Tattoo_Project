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
        private readonly TimeProvider timeProvider;

        public ArtistUnavailableDateService(TattooDbContext context, TimeProvider timeProvider)
        {
            this.context = context;
            this.timeProvider = timeProvider;
        }

        public async Task<ResultService> CreateUnavailableDateAsync(CreateArtistUnavailableDateDto dto, string userId)
        {
            await using var bookingTransaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile not found.");
            }

            await BookingConcurrency.AcquireArtistLockAsync(context, tattooArtist.Id);

            var startResult = TimeZoneSupport.ToUtc(dto.StartDateTime, tattooArtist.TimeZoneId);
            var endResult = TimeZoneSupport.ToUtc(dto.EndDateTime, tattooArtist.TimeZoneId);
            if (!startResult.Success) return ResultService.Fail(startResult.ErrorMessage!);
            if (!endResult.Success) return ResultService.Fail(endResult.ErrorMessage!);
            var startTime = startResult.Data;
            var endTime = endResult.Data;

            if (startTime >= endTime)
            {
                return ResultService.Fail("Start date must be before end date.");
            }

            if (startTime < timeProvider.GetUtcNow().UtcDateTime)
            {
                return ResultService.Fail("You cannot mark past time as unavailable.");
            }

            var hasExistingUnavailablePeriod = await context.ArtistUnavailableDates
                .AnyAsync(u =>
                    u.TattooArtistId == tattooArtist.Id &&
                    startTime < u.EndDateTime &&
                    endTime > u.StartDateTime);

            if (hasExistingUnavailablePeriod)
            {
                return ResultService.Fail("You already marked this period as unavailable.");
            }

            var hasConsultationConflict = await context.Consultations
                .AnyAsync(c =>
                    !c.IsCancelled && c.TattooRequest.TattooArtistId == tattooArtist.Id &&
                    startTime < c.EndTime &&
                    endTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return ResultService.Fail("You already have a consultation in this period. Please cancel it before marking this time as unavailable.");
            }

            var hasTattooSessionConflict = await context.TattooSessions
                .AnyAsync(s =>
                    !s.IsCancelled && s.TattooRequest.TattooArtistId == tattooArtist.Id &&
                    startTime < s.EndTime &&
                    endTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return ResultService.Fail("You already have a tattoo session in this period. Please cancel it before marking this time as unavailable.");
            }

            var unavailableDate = new ArtistUnavailableDate
            {
                StartDateTime = startTime,
                EndDateTime = endTime,
                TattooArtistId = tattooArtist.Id
            };

            await context.ArtistUnavailableDates.AddAsync(unavailableDate);
            await context.SaveChangesAsync();
            await bookingTransaction.CommitAsync();

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

            var currentDateTime = timeProvider.GetUtcNow().UtcDateTime;

            var unavailableDates = await context.ArtistUnavailableDates
                .AsNoTracking()
                .Where(u =>
                    u.TattooArtistId == tattooArtist.Id &&
                    u.EndDateTime > currentDateTime)
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
