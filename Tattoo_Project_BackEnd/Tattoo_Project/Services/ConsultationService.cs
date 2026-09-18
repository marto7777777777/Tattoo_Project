using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class ConsultationService(TattooDbContext context, TimeProvider timeProvider)
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
            await using var bookingTransaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile was not found.");
            }

            var tattooRequest = await context.TattooRequests
                .Include(r => r.ArtistResponse)
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

            if (tattooRequest.Status != RequestStatus.Approved ||
                tattooRequest.ArtistResponse?.WorkflowPath != ArtistResponseWorkflowPath.ConsultationFirst)
            {
                return ResultService.Fail("Consultation can be booked only for an approved consultation-first request.");
            }

            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.Id == tattooRequest.TattooArtistId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            await BookingConcurrency.AcquireArtistLockAsync(context, tattooArtist.Id);

            if (tattooArtist.ConsultationDurationMinutes < 15)
            {
                return ResultService.Fail("Tattoo artist consultation duration is invalid.");
            }

            var startResult = TimeZoneSupport.ToUtc(dto.StartTime, tattooArtist.TimeZoneId);
            if (!startResult.Success) return ResultService.Fail(startResult.ErrorMessage!);
            var startTime = startResult.Data;
            var endTime = startTime.AddMinutes(tattooArtist.ConsultationDurationMinutes);

            if (startTime <= timeProvider.GetUtcNow().UtcDateTime)
            {
                return ResultService.Fail("Consultation cannot be booked in the past.");
            }

            var alreadyHasConsultation = await context.Consultations
                .AnyAsync(c => c.TattooRequestId == dto.TattooRequestId && !c.IsCancelled);

            if (alreadyHasConsultation)
            {
                return ResultService.Fail(
                    "This tattoo request already has a consultation.");
            }

            var tattooArtistId = tattooRequest.TattooArtistId;

            var isWithinSchedule = await IsArtistAvailableInScheduleAsync(
                tattooArtistId,
                startTime,
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
                startTime < u.EndDateTime &&
                endTime > u.StartDateTime);

            if (isArtistUnavailable)
            {
                return ResultService.Fail("Artist is unavailable during this period.");
            }

            var hasConsultationConflict = await context.Consultations.AnyAsync(c =>
                !c.IsCancelled && c.TattooRequest.TattooArtistId == tattooArtistId &&
                startTime < c.EndTime &&
                endTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has a consultation at this time.");
            }

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                !s.IsCancelled && s.TattooRequest.TattooArtistId == tattooArtistId &&
                startTime < s.EndTime &&
                endTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has a tattoo session at this time.");
            }

            Consultation consultation = new()
            {
                TattooRequestId = dto.TattooRequestId,
                StartTime = startTime,
                EndTime = endTime,
                Notes = dto.Notes,
                IsCompleted = false
            };

            context.Consultations.Add(consultation);
            var transition = TattooRequestStateMachine.Transition(tattooRequest, RequestStatus.WaitingForConsultation);
            if (!transition.Success) return transition;

            await context.SaveChangesAsync();
            await bookingTransaction.CommitAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateConsultationAsync(
            int id,
            UpdateConsultationDto dto,
            string userId)
        {
            await using var bookingTransaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
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

            if (consultation.IsCompleted || consultation.IsCancelled)
            {
                return ResultService.Fail("Completed or cancelled consultation cannot be updated.");
            }

            if (consultation.StartTime <= timeProvider.GetUtcNow().UtcDateTime.AddHours(24))
            {
                return ResultService.Fail(
                    "Consultation can be updated only at least 24 hours before its start time.");
            }

            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.Id == consultation.TattooRequest.TattooArtistId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            await BookingConcurrency.AcquireArtistLockAsync(context, tattooArtist.Id);

            if (tattooArtist.ConsultationDurationMinutes < 15)
            {
                return ResultService.Fail("Tattoo artist consultation duration is invalid.");
            }

            var startResult = TimeZoneSupport.ToUtc(dto.StartTime, tattooArtist.TimeZoneId);
            if (!startResult.Success) return ResultService.Fail(startResult.ErrorMessage!);
            var startTime = startResult.Data;
            if (startTime <= timeProvider.GetUtcNow().UtcDateTime) return ResultService.Fail("Consultation cannot be moved to the past.");
            var endTime = startTime.AddMinutes(tattooArtist.ConsultationDurationMinutes);

            var tattooArtistId = consultation.TattooRequest.TattooArtistId;

            var isWithinSchedule = await IsArtistAvailableInScheduleAsync(
                tattooArtistId,
                startTime,
                endTime,
                ScheduleType.Consultation);

            if (!isWithinSchedule)
            {
                return ResultService.Fail(
                    "The selected consultation time is outside the tattoo artist's consultation schedule.");
            }

            var hasConsultationConflict = await context.Consultations.AnyAsync(c =>
                !c.IsCancelled && c.Id != id &&
                c.TattooRequest.TattooArtistId == tattooArtistId &&
                startTime < c.EndTime &&
                endTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has a consultation at this time.");
            }

            var isArtistUnavailable = await context.ArtistUnavailableDates
            .AnyAsync(u =>
                u.TattooArtistId == tattooArtist.Id &&
                startTime < u.EndDateTime &&
                endTime > u.StartDateTime);

            if (isArtistUnavailable)
            {
                return ResultService.Fail("Artist is unavailable during this period.");
            }

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                !s.IsCancelled && s.TattooRequest.TattooArtistId == tattooArtistId &&
                startTime < s.EndTime &&
                endTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has a tattoo session at this time.");
            }

            consultation.StartTime = startTime;
            consultation.EndTime = endTime;
            consultation.Notes = dto.Notes;

            await context.SaveChangesAsync();
            await bookingTransaction.CommitAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> DeleteConsultationAsync(int id,string userId,bool isAdmin)
        {
            await using var transaction=await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var artistIdForLock=await context.Consultations.Where(c=>c.Id==id).Select(c=>(int?)c.TattooRequest.TattooArtistId).FirstOrDefaultAsync();
            if(artistIdForLock==null)return ResultService.Fail("Consultation was not found.");
            await BookingConcurrency.AcquireArtistLockAsync(context,artistIdForLock.Value);
            var requestId=await context.Consultations.Where(c=>c.Id==id).Select(c=>c.TattooRequestId).FirstAsync();
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{requestId}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");
            var tattooArtist=await context.TattooArtists.FirstOrDefaultAsync(a=>a.UserId==userId);var client=await context.Clients.FirstOrDefaultAsync(c=>c.UserId==userId);
            var consultation=await context.Consultations.Include(c=>c.TattooRequest).FirstOrDefaultAsync(c=>c.Id==id);if(consultation==null)return ResultService.Fail("Consultation was not found.");
            var isAssignedArtist=tattooArtist!=null&&consultation.TattooRequest.TattooArtistId==tattooArtist.Id;var isOwningClient=client!=null&&consultation.TattooRequest.ClientId==client.Id;
            if(!isAdmin&&!isAssignedArtist&&!isOwningClient)return ResultService.Fail("You can cancel only consultations that belong to your tattoo request.");
            if(consultation.IsCompleted||consultation.IsCancelled||consultation.TattooRequest.Status!=RequestStatus.WaitingForConsultation)return ResultService.Fail("A completed or cancelled consultation cannot be cancelled.");
            var now=timeProvider.GetUtcNow().UtcDateTime;if(consultation.StartTime<=now)return ResultService.Fail("A consultation that has already started cannot be cancelled.");
            if(isOwningClient&&!isAssignedArtist&&!isAdmin&&consultation.StartTime<=now.AddHours(24))return ResultService.Fail("Clients can cancel a consultation only more than 24 hours before it starts.");
            consultation.IsCancelled=true;consultation.CancelledAt=now;consultation.CancelledByUserId=userId;consultation.CancellationReason=isOwningClient&&!isAssignedArtist&&!isAdmin?"Cancelled by client.":"Cancelled by artist or administrator.";
            var transition=TattooRequestStateMachine.Transition(consultation.TattooRequest,RequestStatus.Approved);if(!transition.Success)return transition;
            await context.SaveChangesAsync();await transaction.CommitAsync();return ResultService.Ok();
        }

        public async Task<ResultService> CompleteConsultationAsync(
            int tattooRequestId,
            CompleteConsultationDto dto,
            string userId)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }
            await BookingConcurrency.AcquireArtistLockAsync(context,tattooArtist.Id);
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{tattooRequestId}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");

            var tattooRequest = await context.TattooRequests
                .Include(r => r.Consultation)
                .Include(r => r.TattooSessions)
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
                .FirstOrDefaultAsync(c => c.TattooRequestId == tattooRequestId && !c.IsCancelled);

            if (consultation == null)
            {
                return ResultService.Fail("Consultation was not found.");
            }

            if (consultation.IsCompleted)
            {
                return ResultService.Fail("Consultation is already completed.");
            }
            if (consultation.EndTime > timeProvider.GetUtcNow().UtcDateTime)
            {
                return ResultService.Fail("Consultation cannot be completed before its scheduled end time.");
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

            var transition = TattooRequestStateMachine.Transition(tattooRequest, RequestStatus.ConsultationCompleted);
            if (!transition.Success) return transition;
            tattooRequest.RemainingSessionsToBook = dto.SessionsToBook;
            tattooRequest.PriceForSession = dto.PriceForSession;
            tattooRequest.DurationHoursForSession = dto.DurationHoursForSession;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> RejectConsultationAsync(int tattooRequestId, string userId)
        {
            await using var transaction=await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var tattooArtist=await context.TattooArtists.FirstOrDefaultAsync(a=>a.UserId==userId);if(tattooArtist==null)return ResultService.Fail("Tattoo artist profile was not found.");
            await BookingConcurrency.AcquireArtistLockAsync(context,tattooArtist.Id);
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{tattooRequestId}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");
            var tattooRequest=await context.TattooRequests.Include(r=>r.Consultation).Include(r=>r.TattooSessions).FirstOrDefaultAsync(r=>r.Id==tattooRequestId);
            if(tattooRequest==null||tattooRequest.TattooArtistId!=tattooArtist.Id)return ResultService.Fail("Tattoo request was not found.");
            if(tattooRequest.Status is RequestStatus.Completed or RequestStatus.Rejected or RequestStatus.Cancelled)return ResultService.Fail("This tattoo request cannot be cancelled from the current status.");
            var transition=TattooRequestStateMachine.Transition(tattooRequest,RequestStatus.Cancelled);if(!transition.Success)return transition;var now=timeProvider.GetUtcNow().UtcDateTime;tattooRequest.CancelledAt=now;tattooRequest.CancelledByUserId=userId;tattooRequest.CancellationReason="Cancelled by artist after approval.";
            if(tattooRequest.Consultation is {IsCancelled:false,IsCompleted:false} c){c.IsCancelled=true;c.CancelledAt=now;c.CancelledByUserId=userId;c.CancellationReason="Cancelled with tattoo request.";}foreach(var session in tattooRequest.TattooSessions.Where(x=>!x.IsCancelled&&x.StartTime>now)){session.IsCancelled=true;session.CancelledAt=now;session.CancelledByUserId=userId;session.CancellationReason="Cancelled with tattoo request.";}
            await context.SaveChangesAsync();await transaction.CommitAsync();return ResultService.Ok();
        }

        private static GetConsultationDto MapToGetConsultationDto(Consultation consultation)
        {
            return new GetConsultationDto
            {
                StartTime = consultation.StartTime,
                EndTime = consultation.EndTime,
                Notes = consultation.Notes,
                IsCompleted = consultation.IsCompleted,
                IsCancelled = consultation.IsCancelled,
                CancelledAt = consultation.CancelledAt
            };
        }

        private async Task<bool> IsArtistAvailableInScheduleAsync(
            int tattooArtistId, DateTime startTimeUtc, DateTime endTimeUtc, ScheduleType scheduleType)
        {
            var artist = await context.TattooArtists.AsNoTracking()
                .Where(a => a.Id == tattooArtistId)
                .Select(a => new { a.TimeZoneId })
                .FirstOrDefaultAsync();
            if (artist == null) return false;
            var zoneResult = TimeZoneSupport.Get(artist.TimeZoneId);
            if (!zoneResult.Success) return false;
            var zone = zoneResult.Data!;
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc), zone);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(endTimeUtc, DateTimeKind.Utc), zone);
            if (localStart.Date != localEnd.Date) return false;
            var requestedStartTime = TimeOnly.FromDateTime(localStart);
            var requestedEndTime = TimeOnly.FromDateTime(localEnd);
            var schedules = await context.Schedules.AsNoTracking().Where(s =>
                s.TattooArtistId == tattooArtistId && s.DayOfWeek == localStart.DayOfWeek && s.ScheduleType == scheduleType).ToListAsync();
            return schedules.Any(s => requestedStartTime >= s.StartTime && requestedEndTime <= s.EndTime);
        }
    }
}
