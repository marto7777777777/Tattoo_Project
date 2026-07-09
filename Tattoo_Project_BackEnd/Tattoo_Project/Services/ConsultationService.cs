using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class ConsultationService(TattooDbContext context)
        : IConsultationService
    {
        public async Task<ResultService<ICollection<GetConsultationDto>>> GetAllConsultationsAsync()
        {
            var consultations = await context.Consultations
                .Select(c => new GetConsultationDto
                {
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    Notes = c.Notes,
                    IsCompleted = c.IsCompleted
                })
                .ToListAsync();

            return ResultService<ICollection<GetConsultationDto>>.Ok(consultations);
        }

        public async Task<ResultService<GetConsultationDto>> GetConsultationByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist)
        {
            var consultation = await context.Consultations
                .Include(c => c.TattooRequest)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consultation == null)
            {
                return ResultService<GetConsultationDto>.Fail(
                    "Consultation was not found.");
            }

            if (isAdmin)
            {
                return ResultService<GetConsultationDto>.Ok(MapToGetConsultationDto(consultation));
            }

            if (isClient)
            {
                var client = await context.Clients
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (client != null &&
                    consultation.TattooRequest.ClientId == client.Id)
                {
                    return ResultService<GetConsultationDto>.Ok(
                        MapToGetConsultationDto(consultation));
                }
            }

            if (isArtist)
            {
                var tattooArtist = await context.TattooArtists
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (tattooArtist != null &&
                    consultation.TattooRequest.TattooArtistId == tattooArtist.Id)
                {
                    return ResultService<GetConsultationDto>.Ok(
                        MapToGetConsultationDto(consultation));
                }
            }

            return ResultService<GetConsultationDto>.Fail(
                "You do not have permission to view this consultation.");
        }

        public async Task<ResultService> CreateConsultationAsync(
            CreateConsultationDto dto,
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
                    "You can create consultation only for your own tattoo requests.");
            }

            if (tattooRequest.Status != RequestStatus.WaitingForConsultation)
            {
                return ResultService.Fail(
                    "Consultation can be created only after artist response.");
            }

            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.Id == tattooRequest.TattooArtistId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            if (tattooArtist.ConsultationDurationMinutes < 15)
            {
                return ResultService.Fail("Tattoo artist consultation duration is invalid.");
            }

            var endTime = dto.StartTime.AddMinutes(tattooArtist.ConsultationDurationMinutes);

            if (dto.StartTime <= DateTime.UtcNow)
            {
                return ResultService.Fail("Consultation cannot be booked in the past.");
            }

            var alreadyHasConsultation = await context.Consultations
                .AnyAsync(c => c.TattooRequestId == dto.TattooRequestId);

            if (alreadyHasConsultation)
            {
                return ResultService.Fail(
                    "This tattoo request already has a consultation.");
            }

            var tattooArtistId = tattooRequest.TattooArtistId;

            var isWithinSchedule = await IsArtistAvailableInScheduleAsync(
                tattooArtistId,
                dto.StartTime,
                endTime,
                ScheduleType.Consultation);

            if (!isWithinSchedule)
            {
                return ResultService.Fail(
                    "The selected consultation time is outside the tattoo artist's consultation schedule.");
            }

            var isArtistUnavailable = await context.ArtistUnavailableDates
            .AnyAsync(u =>
                u.TattooArtistId == tattooArtist.Id &&
                dto.StartTime < u.EndDateTime &&
                endTime > u.StartDateTime);

            if (isArtistUnavailable)
            {
                return ResultService.Fail("Artist is unavailable during this period.");
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

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < s.EndTime &&
                endTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has a tattoo session at this time.");
            }

            Consultation consultation = new()
            {
                TattooRequestId = dto.TattooRequestId,
                StartTime = dto.StartTime,
                EndTime = endTime,
                Notes = dto.Notes,
                IsCompleted = false
            };

            context.Consultations.Add(consultation);

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateConsultationAsync(
            int id,
            UpdateConsultationDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile was not found.");
            }

            var consultation = await context.Consultations
                .Include(c => c.TattooRequest)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consultation == null)
            {
                return ResultService.Fail("Consultation was not found.");
            }

            if (consultation.TattooRequest.ClientId != client.Id)
            {
                return ResultService.Fail(
                    "You can update only your own consultations.");
            }

            if (consultation.IsCompleted)
            {
                return ResultService.Fail("Completed consultation cannot be updated.");
            }

            if (consultation.StartTime <= DateTime.UtcNow.AddHours(24))
            {
                return ResultService.Fail(
                    "Consultation can be updated only at least 24 hours before its start time.");
            }

            if (dto.StartTime <= DateTime.UtcNow)
            {
                return ResultService.Fail("Consultation cannot be moved to the past.");
            }

            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.Id == consultation.TattooRequest.TattooArtistId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            if (tattooArtist.ConsultationDurationMinutes < 15)
            {
                return ResultService.Fail("Tattoo artist consultation duration is invalid.");
            }

            var endTime = dto.StartTime.AddMinutes(tattooArtist.ConsultationDurationMinutes);

            var tattooArtistId = consultation.TattooRequest.TattooArtistId;

            var isWithinSchedule = await IsArtistAvailableInScheduleAsync(
                tattooArtistId,
                dto.StartTime,
                endTime,
                ScheduleType.Consultation);

            if (!isWithinSchedule)
            {
                return ResultService.Fail(
                    "The selected consultation time is outside the tattoo artist's consultation schedule.");
            }

            var hasConsultationConflict = await context.Consultations.AnyAsync(c =>
                c.Id != id &&
                c.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < c.EndTime &&
                endTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has a consultation at this time.");
            }

            var isArtistUnavailable = await context.ArtistUnavailableDates
            .AnyAsync(u =>
                u.TattooArtistId == tattooArtist.Id &&
                dto.StartTime < u.EndDateTime &&
                endTime > u.StartDateTime);

            if (isArtistUnavailable)
            {
                return ResultService.Fail("Artist is unavailable during this period.");
            }

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < s.EndTime &&
                endTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has a tattoo session at this time.");
            }

            consultation.StartTime = dto.StartTime;
            consultation.EndTime = endTime;
            consultation.Notes = dto.Notes;

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> DeleteConsultationAsync(
            int id,
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            var consultation = await context.Consultations
                .Include(c => c.TattooRequest)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consultation == null)
            {
                return ResultService.Fail("Consultation was not found.");
            }

            if (consultation.TattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return ResultService.Fail(
                    "You can delete only consultations assigned to you.");
            }

            context.Consultations.Remove(consultation);

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> CompleteConsultationAsync(
            int tattooRequestId,
            CompleteConsultationDto dto,
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
                    "You can complete consultations only for your own tattoo requests.");
            }

            if (tattooRequest.Status != RequestStatus.WaitingForConsultation)
            {
                return ResultService.Fail(
                    "Consultation can be completed only while request is waiting for consultation.");
            }

            var consultation = await context.Consultations
                .FirstOrDefaultAsync(c => c.TattooRequestId == tattooRequestId);

            if (consultation == null)
            {
                return ResultService.Fail("Consultation was not found.");
            }

            if (consultation.IsCompleted)
            {
                return ResultService.Fail("Consultation is already completed.");
            }

            if (dto.SessionsToBook <= 0)
            {
                return ResultService.Fail("Sessions to book must be greater than zero.");
            }

            if (dto.PriceForSession == null ||
                dto.PriceForSession.Count != dto.SessionsToBook)
            {
                return ResultService.Fail(
                    "Price count must match the number of sessions to book.");
            }

            if (dto.PriceForSession.Any(price => price <= 0))
            {
                return ResultService.Fail("Every session price must be greater than zero.");
            }

            if (dto.DurationHoursForSession == null ||
                dto.DurationHoursForSession.Count != dto.SessionsToBook)
            {
                return ResultService.Fail(
                    "Duration count must match the number of sessions to book.");
            }

            if (dto.DurationHoursForSession.Any(duration => duration <= 0))
            {
                return ResultService.Fail("Every session duration must be greater than zero.");
            }

            consultation.IsCompleted = true;

            tattooRequest.Status = RequestStatus.ConsultationCompleted;
            tattooRequest.RemainingSessionsToBook = dto.SessionsToBook;
            tattooRequest.PriceForSession = dto.PriceForSession;
            tattooRequest.DurationHoursForSession = dto.DurationHoursForSession;

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> RejectConsultationAsync(
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
                    "You can reject only consultations assigned to you.");
            }

            if (tattooRequest.Status != RequestStatus.WaitingForConsultation &&
                tattooRequest.Status != RequestStatus.ConsultationCompleted)
            {
                return ResultService.Fail(
                    "This tattoo request cannot be rejected from the current status.");
            }

            var consultation = await context.Consultations
                .FirstOrDefaultAsync(c => c.TattooRequestId == tattooRequestId);

            if (consultation == null)
            {
                return ResultService.Fail("Consultation was not found.");
            }

            tattooRequest.Status = RequestStatus.Rejected;

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        private static GetConsultationDto MapToGetConsultationDto(Consultation consultation)
        {
            return new GetConsultationDto
            {
                StartTime = consultation.StartTime,
                EndTime = consultation.EndTime,
                Notes = consultation.Notes,
                IsCompleted = consultation.IsCompleted
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