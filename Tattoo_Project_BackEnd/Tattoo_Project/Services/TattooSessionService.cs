using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class TattooSessionService(TattooDbContext context, TimeProvider timeProvider)
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
                .Include(s => s.TattooRequest).ThenInclude(r => r.ArtistResponse)
                .Include(s => s.TattooRequest).ThenInclude(r => r.Consultation)
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
                    "You can book tattoo sessions only for your own tattoo requests.");
            }

            await BookingConcurrency.AcquireArtistLockAsync(context, tattooRequest.TattooArtistId);

            var artistTimeZoneId = await context.TattooArtists
                .Where(a => a.Id == tattooRequest.TattooArtistId)
                .Select(a => a.TimeZoneId)
                .FirstOrDefaultAsync();
            var startResult = TimeZoneSupport.ToUtc(dto.StartTime, artistTimeZoneId);
            if (!startResult.Success) return ResultService.Fail(startResult.ErrorMessage!);
            var startTime = startResult.Data;

            if (startTime <= timeProvider.GetUtcNow().UtcDateTime)
            {
                return ResultService.Fail("Tattoo session cannot be booked in the past.");
            }

            var approvedDirect = tattooRequest.Status == RequestStatus.Approved &&
                                 tattooRequest.ArtistResponse?.WorkflowPath == ArtistResponseWorkflowPath.DirectToSessions;
            if (!approvedDirect && tattooRequest.Status is not (RequestStatus.ConsultationCompleted or RequestStatus.TattooBooked or RequestStatus.InProgress))
            {
                return ResultService.Fail("Tattoo session cannot be booked for the current tattoo request status.");
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
                .CountAsync(s => s.TattooRequestId == dto.TattooRequestId && !s.IsCancelled);

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

            var endTime = startTime.AddHours(durationHours);

            if (endTime - startTime < TimeSpan.FromMinutes(15))
            {
                return ResultService.Fail("Tattoo session must be at least 15 minutes long.");
            }

            var tattooArtistId = tattooRequest.TattooArtistId;

            var isWithinSchedule = await IsArtistAvailableInScheduleAsync(
                tattooArtistId,
                startTime,
                endTime,
                ScheduleType.TattooSession);

            if (!isWithinSchedule)
            {
                return ResultService.Fail(
                    "The selected tattoo session time is outside the tattoo artist's working schedule.");
            }

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                !s.IsCancelled && s.TattooRequest.TattooArtistId == tattooArtistId &&
                startTime < s.EndTime &&
                endTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has another tattoo session at this time.");
            }

            var isArtistUnavailable = await context.ArtistUnavailableDates
            .AnyAsync(u =>
                u.TattooArtistId == tattooRequest.TattooArtistId &&
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

            TattooSession tattooSession = new()
            {
                TattooRequestId = dto.TattooRequestId,
                StartTime = startTime,
                EndTime = endTime,
                DurationHours = durationHours,
                PriceForTheSession = price
            };

            context.TattooSessions.Add(tattooSession);

            tattooRequest.RemainingSessionsToBook--;

            if (tattooRequest.Status is RequestStatus.Approved or RequestStatus.ConsultationCompleted)
            {
                var transition = TattooRequestStateMachine.Transition(tattooRequest, RequestStatus.TattooBooked);
                if (!transition.Success) return transition;
            }

            await context.SaveChangesAsync();
            await bookingTransaction.CommitAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateTattooSessionAsync(
            int id,
            UpdateTattooSessionDto dto,
            string userId)
        {
            await using var bookingTransaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
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

            if (tattooSession.StartTime <= timeProvider.GetUtcNow().UtcDateTime.AddHours(24))
            {
                return ResultService.Fail(
                    "Tattoo session can be updated only at least 24 hours before its start time.");
            }

            if (tattooSession.IsCancelled) return ResultService.Fail("Cancelled tattoo session cannot be updated.");

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

            var tattooArtistId = tattooSession.TattooRequest.TattooArtistId;
            await BookingConcurrency.AcquireArtistLockAsync(context, tattooArtistId);

            var artistTimeZoneId = await context.TattooArtists.Where(a => a.Id == tattooArtistId).Select(a => a.TimeZoneId).FirstOrDefaultAsync();
            var startResult = TimeZoneSupport.ToUtc(dto.StartTime, artistTimeZoneId);
            if (!startResult.Success) return ResultService.Fail(startResult.ErrorMessage!);
            var startTime = startResult.Data;
            if (startTime <= timeProvider.GetUtcNow().UtcDateTime) return ResultService.Fail("Tattoo session cannot be moved to the past.");
            var endTime = startTime.AddHours(Convert.ToDouble(tattooSession.DurationHours));
            if (endTime - startTime < TimeSpan.FromMinutes(15)) return ResultService.Fail("Tattoo session must be at least 15 minutes long.");

            var isWithinSchedule = await IsArtistAvailableInScheduleAsync(
                tattooArtistId,
                startTime,
                endTime,
                ScheduleType.TattooSession);

            if (!isWithinSchedule)
            {
                return ResultService.Fail(
                    "The selected tattoo session time is outside the tattoo artist's working schedule.");
            }

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                !s.IsCancelled && s.Id != id &&
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                startTime < s.EndTime &&
                endTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return ResultService.Fail(
                    "Tattoo artist already has another tattoo session at this time.");
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

            var isArtistUnavailable = await context.ArtistUnavailableDates
            .AnyAsync(u =>
                u.TattooArtistId == tattooSession.TattooRequest.TattooArtistId &&
                startTime < u.EndDateTime &&
                endTime > u.StartDateTime);

            if (isArtistUnavailable)
            {
                return ResultService.Fail("Artist is unavailable during this period.");
            }

            tattooSession.StartTime = startTime;
            tattooSession.EndTime = endTime;

            await context.SaveChangesAsync();
            await bookingTransaction.CommitAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> DeleteTattooSessionAsync(int id,string userId,bool isAdmin)
        {
            await using var transaction=await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var artistIdForLock=await context.TattooSessions.Where(s=>s.Id==id).Select(s=>(int?)s.TattooRequest.TattooArtistId).FirstOrDefaultAsync();
            if(artistIdForLock==null)return ResultService.Fail("Tattoo session was not found.");
            await BookingConcurrency.AcquireArtistLockAsync(context,artistIdForLock.Value);
            var requestId=await context.TattooSessions.Where(s=>s.Id==id).Select(s=>s.TattooRequestId).FirstAsync();
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{requestId}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");
            var tattooArtist=await context.TattooArtists.FirstOrDefaultAsync(a=>a.UserId==userId);
            var client=await context.Clients.FirstOrDefaultAsync(c=>c.UserId==userId);
            var tattooSession=await context.TattooSessions.Include(s=>s.TattooRequest).ThenInclude(r=>r.Consultation).FirstOrDefaultAsync(s=>s.Id==id);
            if(tattooSession==null)return ResultService.Fail("Tattoo session was not found.");
            var tattooRequest=tattooSession.TattooRequest;var isAssignedArtist=tattooArtist!=null&&tattooRequest.TattooArtistId==tattooArtist.Id;var isOwningClient=client!=null&&tattooRequest.ClientId==client.Id;
            if(!isAdmin&&!isAssignedArtist&&!isOwningClient)return ResultService.Fail("You can cancel only tattoo sessions that belong to your tattoo request.");
            if(tattooRequest.Status is RequestStatus.Completed or RequestStatus.Rejected or RequestStatus.Cancelled)return ResultService.Fail("Tattoo sessions cannot be cancelled for a closed request.");
            if(tattooSession.IsCancelled)return ResultService.Fail("Tattoo session is already cancelled.");
            var now=timeProvider.GetUtcNow().UtcDateTime;if(tattooSession.StartTime<=now)return ResultService.Fail("A tattoo session that has already started cannot be cancelled.");
            if(isOwningClient&&!isAssignedArtist&&!isAdmin&&tattooSession.StartTime<=now.AddHours(24))return ResultService.Fail("Clients can cancel a tattoo session only more than 24 hours before it starts.");
            var otherBookedSessionsCount=await context.TattooSessions.CountAsync(s=>s.TattooRequestId==tattooRequest.Id&&s.Id!=tattooSession.Id&&!s.IsCancelled);
            RestoreCancelledSessionPlan(tattooRequest,tattooSession,otherBookedSessionsCount);
            var plannedSessionsCount=Math.Min(tattooRequest.PriceForSession?.Count??0,tattooRequest.DurationHoursForSession?.Count??0);tattooRequest.RemainingSessionsToBook=Math.Min(plannedSessionsCount,Math.Max(0,tattooRequest.RemainingSessionsToBook??0)+1);
            if(tattooRequest.Status==RequestStatus.TattooBooked&&otherBookedSessionsCount==0){var priorStatus=tattooRequest.Consultation is {IsCompleted:true,IsCancelled:false}?RequestStatus.ConsultationCompleted:RequestStatus.Approved;var transition=TattooRequestStateMachine.Transition(tattooRequest,priorStatus);if(!transition.Success)return transition;}
            tattooSession.IsCancelled=true;tattooSession.CancelledAt=now;tattooSession.CancelledByUserId=userId;tattooSession.CancellationReason=isOwningClient&&!isAssignedArtist&&!isAdmin?"Cancelled by client.":"Cancelled by artist or administrator.";
            await context.SaveChangesAsync();await transaction.CommitAsync();return ResultService.Ok();
        }

        private static void RestoreCancelledSessionPlan(
            TattooRequest tattooRequest,
            TattooSession tattooSession,
            int otherBookedSessionsCount)
        {
            var prices = tattooRequest.PriceForSession;
            var durations = tattooRequest.DurationHoursForSession;
            if (prices == null || durations == null || prices.Count != durations.Count) return;

            var cancelledPlanIndex = -1;
            for (var index = 0; index < prices.Count; index++)
            {
                if (prices[index] == tattooSession.PriceForTheSession &&
                    durations[index] == tattooSession.DurationHours)
                {
                    cancelledPlanIndex = index;
                    break;
                }
            }

            if (cancelledPlanIndex < 0) return;

            var cancelledPrice = prices[cancelledPlanIndex];
            var cancelledDuration = durations[cancelledPlanIndex];
            prices.RemoveAt(cancelledPlanIndex);
            durations.RemoveAt(cancelledPlanIndex);

            var rebookingIndex = Math.Min(otherBookedSessionsCount, prices.Count);
            prices.Insert(rebookingIndex, cancelledPrice);
            durations.Insert(rebookingIndex, cancelledDuration);
        }

        public async Task<ResultService> AddMoreSessionsAsync(
            int tattooRequestId,
            AddAdditionalSessionsDto dto,
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
            await transaction.CommitAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> StartTattooAsync(int tattooRequestId, string userId)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var artist = await context.TattooArtists.FirstOrDefaultAsync(a => a.UserId == userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");
            await BookingConcurrency.AcquireArtistLockAsync(context,artist.Id);
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{tattooRequestId}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");
            var request = await context.TattooRequests.Include(r => r.TattooSessions).FirstOrDefaultAsync(r => r.Id == tattooRequestId);
            if (request == null || request.TattooArtistId != artist.Id) return ResultService.Fail("Tattoo request was not found.");
            if (request.Status != RequestStatus.TattooBooked) return ResultService.Fail("Tattoo can be started only from TattooBooked status.");
            var activeSessions = request.TattooSessions.Where(s => !s.IsCancelled).OrderBy(s => s.StartTime).ToList();
            if (activeSessions.Count == 0) return ResultService.Fail("Tattoo cannot be started without a booked session.");
            if (activeSessions[0].StartTime > timeProvider.GetUtcNow().UtcDateTime) return ResultService.Fail("Tattoo cannot be started before the first booked session begins.");
            var transition = TattooRequestStateMachine.Transition(request, RequestStatus.InProgress);
            if (!transition.Success) return transition;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> CompleteTattooAsync(
            int tattooRequestId,
            string userId)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var tattooArtist = await context.TattooArtists.FirstOrDefaultAsync(a => a.UserId == userId);
            if (tattooArtist == null) return ResultService.Fail("Tattoo artist profile was not found.");
            await BookingConcurrency.AcquireArtistLockAsync(context,tattooArtist.Id);
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{tattooRequestId}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");

            var tattooRequest = await context.TattooRequests
                .Include(r => r.TattooSessions)
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);
            if (tattooRequest == null) return ResultService.Fail("Tattoo request was not found.");
            if (tattooRequest.TattooArtistId != tattooArtist.Id) return ResultService.Fail("You can complete only tattoo requests assigned to you.");
            if (tattooRequest.Status == RequestStatus.Completed) return ResultService.Ok();
            if (tattooRequest.Status != RequestStatus.InProgress)
                return ResultService.Fail("Tattoo can be completed only after it has explicitly entered InProgress status.");

            var sessions = tattooRequest.TattooSessions?.Where(s => !s.IsCancelled).ToList() ?? new List<TattooSession>();
            if (sessions.Count == 0) return ResultService.Fail("Tattoo cannot be completed without any tattoo sessions.");
            if ((tattooRequest.RemainingSessionsToBook ?? 0) != 0)
                return ResultService.Fail("Tattoo cannot be completed while there are remaining sessions to book.");
            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (sessions.Any(s => s.EndTime > now))
                return ResultService.Fail("Tattoo cannot be completed before all booked sessions have ended.");

            var transition = TattooRequestStateMachine.Transition(tattooRequest, RequestStatus.Completed);
            if (!transition.Success) return transition;
            var completedCount = await context.TattooRequests.CountAsync(r => r.TattooArtistId == tattooArtist.Id && r.Status == RequestStatus.Completed) + 1;
            context.AnalyticsOutboxEvents.Add(new AnalyticsOutboxEvent { TattooArtistId = tattooArtist.Id, Name = "project_completed", CompletedProjectCount = completedCount, CreatedAt = now });
            var milestoneName = completedCount == 1 ? AnalyticsMilestones.FirstProjectCompleted : completedCount == 10 ? AnalyticsMilestones.TenthProjectCompleted : null;
            if (milestoneName != null && !await context.ArtistAnalyticsMilestones.AnyAsync(x => x.TattooArtistId == tattooArtist.Id && x.Name == milestoneName))
            {
                context.ArtistAnalyticsMilestones.Add(new ArtistAnalyticsMilestone { TattooArtistId = tattooArtist.Id, Name = milestoneName, OccurredAt = now });
                context.AnalyticsOutboxEvents.Add(new AnalyticsOutboxEvent { TattooArtistId = tattooArtist.Id, Name = milestoneName, CompletedProjectCount = completedCount, CreatedAt = now });
            }
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> ContinueTattooAsync(int tattooRequestId, string userId)
        {
            await using var transaction=await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var artistId=await context.TattooArtists.Where(x=>x.UserId==userId).Select(x=>(int?)x.Id).FirstOrDefaultAsync();
            if(artistId==null)return ResultService.Fail("Tattoo artist profile was not found.");
            await BookingConcurrency.AcquireArtistLockAsync(context,artistId.Value);
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{tattooRequestId}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");
            var tattooRequest=await context.TattooRequests.FirstOrDefaultAsync(r=>r.Id==tattooRequestId);
            if(tattooRequest==null||tattooRequest.TattooArtistId!=artistId.Value)return ResultService.Fail("Tattoo request was not found.");
            if(tattooRequest.Status!=RequestStatus.Completed)return ResultService.Fail("Only completed tattoos can be continued.");
            var transition=TattooRequestStateMachine.Transition(tattooRequest,RequestStatus.InProgress);if(!transition.Success)return transition;
            await context.SaveChangesAsync();await transaction.CommitAsync();return ResultService.Ok();
        }

        private static GetTattooSessionDto MapToDto(TattooSession tattooSession)
        {
            return new GetTattooSessionDto
            {
                TattooRequestId = tattooSession.TattooRequestId,
                StartTime = tattooSession.StartTime,
                EndTime = tattooSession.EndTime,
                DurationHours = tattooSession.DurationHours,
                PriceForTheSession = tattooSession.PriceForTheSession,
                IsCancelled = tattooSession.IsCancelled,
                CancelledAt = tattooSession.CancelledAt
            };
        }

        private async Task<bool> IsArtistAvailableInScheduleAsync(
            int tattooArtistId, DateTime startTimeUtc, DateTime endTimeUtc, ScheduleType scheduleType)
        {
            var artist = await context.TattooArtists.AsNoTracking().Where(a => a.Id == tattooArtistId).Select(a => new { a.TimeZoneId }).FirstOrDefaultAsync();
            if (artist == null) return false;
            var zoneResult = TimeZoneSupport.Get(artist.TimeZoneId);
            if (!zoneResult.Success) return false;
            var zone = zoneResult.Data!;
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc), zone);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(endTimeUtc, DateTimeKind.Utc), zone);
            if (localStart.Date != localEnd.Date) return false;
            var requestedStartTime = TimeOnly.FromDateTime(localStart);
            var requestedEndTime = TimeOnly.FromDateTime(localEnd);
            var schedules = await context.Schedules.AsNoTracking().Where(s => s.TattooArtistId == tattooArtistId && s.DayOfWeek == localStart.DayOfWeek && s.ScheduleType == scheduleType).ToListAsync();
            return schedules.Any(s => requestedStartTime >= s.StartTime && requestedEndTime <= s.EndTime);
        }
    }
}
