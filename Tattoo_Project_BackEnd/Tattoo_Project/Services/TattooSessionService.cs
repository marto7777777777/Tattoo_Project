using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class TattooSessionService(TattooDbContext context)
        : ITattooSessionService
    {
        public async Task<ResultService<ICollection<GetTattooSessionDto>>> GetAllTattooSessionsAsync()
        {
            var sessions = await context.TattooSessions
                .Select(s => new GetTattooSessionDto
                {
                    TattooRequestId = s.TattooRequestId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    DurationHours = s.DurationHours,
                    PriceForTheSession = s.PriceForTheSession
                })
                .ToListAsync();

            return ResultService<ICollection<GetTattooSessionDto>>.Ok(sessions);
        }

        public async Task<ResultService<GetTattooSessionDto>> GetTattooSessionByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist)
        {
            var tattooSession = await context.TattooSessions
                .Include(s => s.TattooRequest)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (tattooSession == null)
            {
                return ResultService<GetTattooSessionDto>.Fail("Tattoo session was not found.");
            }

            if (isAdmin)
            {
                return ResultService<GetTattooSessionDto>.Ok(MapToDto(tattooSession));
            }

            if (isClient)
            {
                var client = await context.Clients
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (client != null &&
                    tattooSession.TattooRequest.ClientId == client.Id)
                {
                    return ResultService<GetTattooSessionDto>.Ok(MapToDto(tattooSession));
                }
            }

            if (isArtist)
            {
                var tattooArtist = await context.TattooArtists
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (tattooArtist != null &&
                    tattooSession.TattooRequest.TattooArtistId == tattooArtist.Id)
                {
                    return ResultService<GetTattooSessionDto>.Ok(MapToDto(tattooSession));
                }
            }

            return ResultService<GetTattooSessionDto>.Fail(
                "You do not have permission to view this tattoo session.");
        }

        public async Task<ResultService> CreateTattooSessionAsync(
            CreateTattooSessionDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile was not found.");
            }

            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == dto.TattooRequestId);

            if (tattooRequest == null)
            {
                return ResultService.Fail("Tattoo request was not found.");
            }

            if (tattooRequest.ClientId != client.Id)
            {
                return ResultService.Fail(
                    "You can book tattoo sessions only for your own tattoo requests.");
            }

            if (dto.StartTime <= DateTime.UtcNow)
            {
                return ResultService.Fail("Tattoo session cannot be booked in the past.");
            }

            if (tattooRequest.Status != RequestStatus.ConsultationCompleted &&
                tattooRequest.Status != RequestStatus.TattooBooked &&
                tattooRequest.Status != RequestStatus.InProgress)
            {
                return ResultService.Fail(
                    "Tattoo session cannot be booked for the current tattoo request status.");
            }

            if (tattooRequest.RemainingSessionsToBook == null ||
                tattooRequest.RemainingSessionsToBook <= 0)
            {
                return ResultService.Fail("There are no remaining sessions to book.");
            }

            if (tattooRequest.PriceForSession == null ||
                !tattooRequest.PriceForSession.Any())
            {
                return ResultService.Fail("No session prices were defined for this tattoo request.");
            }

            if (tattooRequest.DurationHoursForSession == null ||
                !tattooRequest.DurationHoursForSession.Any())
            {
                return ResultService.Fail("No session durations were defined for this tattoo request.");
            }

            if (tattooRequest.PriceForSession.Count != tattooRequest.DurationHoursForSession.Count)
            {
                return ResultService.Fail(
                    "Session prices and session durations count must match.");
            }

            var existingSessionsCount = await context.TattooSessions
                .CountAsync(s => s.TattooRequestId == dto.TattooRequestId);

            if (existingSessionsCount >= tattooRequest.PriceForSession.Count)
            {
                return ResultService.Fail("All planned session prices have already been used.");
            }

            if (existingSessionsCount >= tattooRequest.DurationHoursForSession.Count)
            {
                return ResultService.Fail("All planned session durations have already been used.");
            }

            var price = tattooRequest.PriceForSession[existingSessionsCount];
            var durationHours = tattooRequest.DurationHoursForSession[existingSessionsCount];

            if (durationHours <= 0)
            {
                return ResultService.Fail("Session duration must be greater than zero.");
            }

            var endTime = dto.StartTime.AddHours(durationHours);

            if (endTime - dto.StartTime < TimeSpan.FromMinutes(15))
            {
                return ResultService.Fail("Tattoo session must be at least 15 minutes long.");
            }

            var tattooArtistId = tattooRequest.TattooArtistId;

            var isWithinSchedule = await IsArtistAvailableInScheduleAsync(
                tattooArtistId,
                dto.StartTime,
                endTime,
                ScheduleType.TattooSession);

            if (!isWithinSchedule)
            {
                return ResultService.Fail(
                    "The selected tattoo session time is outside the tattoo artist's working schedule.");
            }

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < s.EndTime &&
                endTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has another tattoo session at this time.");
            }

            var hasConsultationConflict = await context.Consultations.AnyAsync(c =>
                c.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < c.EndTime &&
                endTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has a consultation at this time.");
            }

            TattooSession tattooSession = new()
            {
                TattooRequestId = dto.TattooRequestId,
                StartTime = dto.StartTime,
                EndTime = endTime,
                DurationHours = durationHours,
                PriceForTheSession = price
            };

            context.TattooSessions.Add(tattooSession);

            tattooRequest.RemainingSessionsToBook--;

            if (tattooRequest.Status == RequestStatus.ConsultationCompleted)
            {
                tattooRequest.Status = RequestStatus.TattooBooked;
            }

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateTattooSessionAsync(
            int id,
            UpdateTattooSessionDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile was not found.");
            }

            var tattooSession = await context.TattooSessions
                .Include(s => s.TattooRequest)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (tattooSession == null)
            {
                return ResultService.Fail("Tattoo session was not found.");
            }

            if (tattooSession.TattooRequest.ClientId != client.Id)
            {
                return ResultService.Fail("You can update only your own tattoo sessions.");
            }

            if (tattooSession.StartTime <= DateTime.UtcNow.AddHours(24))
            {
                return ResultService.Fail(
                    "Tattoo session can be updated only at least 24 hours before its start time.");
            }

            if (dto.StartTime <= DateTime.UtcNow)
            {
                return ResultService.Fail("Tattoo session cannot be moved to the past.");
            }

            if (tattooSession.DurationHours <= 0)
            {
                return ResultService.Fail("Tattoo session duration is invalid.");
            }

            if (tattooSession.TattooRequest.Status != RequestStatus.TattooBooked &&
                tattooSession.TattooRequest.Status != RequestStatus.InProgress)
            {
                return ResultService.Fail(
                    "Tattoo session cannot be updated for the current tattoo request status.");
            }

            var endTime = dto.StartTime.AddHours(Convert.ToDouble(tattooSession.DurationHours));

            if (endTime - dto.StartTime < TimeSpan.FromMinutes(15))
            {
                return ResultService.Fail("Tattoo session must be at least 15 minutes long.");
            }

            var tattooArtistId = tattooSession.TattooRequest.TattooArtistId;

            var isWithinSchedule = await IsArtistAvailableInScheduleAsync(
                tattooArtistId,
                dto.StartTime,
                endTime,
                ScheduleType.TattooSession);

            if (!isWithinSchedule)
            {
                return ResultService.Fail(
                    "The selected tattoo session time is outside the tattoo artist's working schedule.");
            }

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.Id != id &&
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < s.EndTime &&
                endTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has another tattoo session at this time.");
            }

            var hasConsultationConflict = await context.Consultations.AnyAsync(c =>
                c.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < c.EndTime &&
                endTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has a consultation at this time.");
            }

            tattooSession.StartTime = dto.StartTime;
            tattooSession.EndTime = endTime;

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> DeleteTattooSessionAsync(
            int id,
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            var tattooSession = await context.TattooSessions
                .Include(s => s.TattooRequest)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (tattooSession == null)
            {
                return ResultService.Fail("Tattoo session was not found.");
            }

            if (tattooSession.TattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return ResultService.Fail(
                    "You can delete only tattoo sessions assigned to you.");
            }

            context.TattooSessions.Remove(tattooSession);

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> AddMoreSessionsAsync(
            int tattooRequestId,
            AddAdditionalSessionsDto dto,
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);

            if (tattooRequest == null)
            {
                return ResultService.Fail("Tattoo request was not found.");
            }

            if (tattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return ResultService.Fail(
                    "You can add sessions only to tattoo requests assigned to you.");
            }

            if (tattooRequest.Status != RequestStatus.TattooBooked &&
                tattooRequest.Status != RequestStatus.InProgress &&
                tattooRequest.Status != RequestStatus.ConsultationCompleted)
            {
                return ResultService.Fail(
                    "Additional sessions cannot be added for the current tattoo request status.");
            }

            if (dto.AdditionalSessions <= 0)
            {
                return ResultService.Fail("Additional sessions must be greater than zero.");
            }

            if (dto.PriceForSession == null ||
                dto.PriceForSession.Count != dto.AdditionalSessions)
            {
                return ResultService.Fail(
                    "Price count must match the number of additional sessions.");
            }

            if (dto.PriceForSession.Any(price => price <= 0))
            {
                return ResultService.Fail("Every session price must be greater than zero.");
            }

            if (dto.DurationHoursForSession == null ||
                dto.DurationHoursForSession.Count != dto.AdditionalSessions)
            {
                return ResultService.Fail(
                    "Duration count must match the number of additional sessions.");
            }

            if (dto.DurationHoursForSession.Any(duration => duration <= 0))
            {
                return ResultService.Fail("Every session duration must be greater than zero.");
            }

            tattooRequest.PriceForSession ??= new List<decimal>();
            tattooRequest.DurationHoursForSession ??= new List<int>();
            tattooRequest.RemainingSessionsToBook ??= 0;

            tattooRequest.RemainingSessionsToBook += dto.AdditionalSessions;

            foreach (var price in dto.PriceForSession)
            {
                tattooRequest.PriceForSession.Add(price);
            }

            foreach (var duration in dto.DurationHoursForSession)
            {
                tattooRequest.DurationHoursForSession.Add(duration);
            }

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> CompleteTattooAsync(
            int tattooRequestId,
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);

            if (tattooRequest == null)
            {
                return ResultService.Fail("Tattoo request was not found.");
            }

            if (tattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return ResultService.Fail(
                    "You can complete only tattoo requests assigned to you.");
            }

            if (tattooRequest.Status != RequestStatus.TattooBooked &&
                tattooRequest.Status != RequestStatus.InProgress)
            {
                return ResultService.Fail(
                    "Tattoo can be completed only after at least one tattoo session is booked.");
            }

            var hasTattooSessions = await context.TattooSessions
                .AnyAsync(s => s.TattooRequestId == tattooRequestId);

            if (!hasTattooSessions)
            {
                return ResultService.Fail("Tattoo cannot be completed without any tattoo sessions.");
            }

            if (tattooRequest.RemainingSessionsToBook != null &&
                tattooRequest.RemainingSessionsToBook > 0)
            {
                return ResultService.Fail(
                    "Tattoo cannot be completed while there are remaining sessions to book.");
            }

            tattooRequest.Status = RequestStatus.Completed;

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> ContinueTattooAsync(int tattooRequestId)
        {
            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);

            if (tattooRequest == null)
            {
                return ResultService.Fail("Tattoo request was not found.");
            }

            if (tattooRequest.Status != RequestStatus.Completed)
            {
                return ResultService.Fail("Only completed tattoos can be continued.");
            }

            tattooRequest.Status = RequestStatus.InProgress;

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        private static GetTattooSessionDto MapToDto(TattooSession tattooSession)
        {
            return new GetTattooSessionDto
            {
                TattooRequestId = tattooSession.TattooRequestId,
                StartTime = tattooSession.StartTime,
                EndTime = tattooSession.EndTime,
                DurationHours = tattooSession.DurationHours,
                PriceForTheSession = tattooSession.PriceForTheSession
            };
        }

        private async Task<bool> IsArtistAvailableInScheduleAsync(
            int tattooArtistId,
            DateTime startTime,
            DateTime endTime,
            ScheduleType scheduleType)
        {
            if (startTime.Date != endTime.Date)
            {
                return false;
            }

            var requestedDay = startTime.DayOfWeek;

            var requestedStartTime = TimeOnly.FromDateTime(startTime);
            var requestedEndTime = TimeOnly.FromDateTime(endTime);

            var schedules = await context.Schedules
                .Where(s =>
                    s.TattooArtistId == tattooArtistId &&
                    s.DayOfWeek == requestedDay &&
                    s.ScheduleType == scheduleType)
                .ToListAsync();

            return schedules.Any(s =>
                requestedStartTime >= s.StartTime &&
                requestedEndTime <= s.EndTime);
        }
    }
}