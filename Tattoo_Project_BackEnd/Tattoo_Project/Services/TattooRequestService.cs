using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ArtistResponceDTOs;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.DTOs.TattooReferenceImageDTOs;
using Tattoo_Project.DTOs.TattooRequestDTOs;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class TattooRequestService(TattooDbContext context, IPrivateMediaUrlService privateMedia, TimeProvider timeProvider, IFileStorage storage, IImageSanitizer imageSanitizer)
        : ITattooRequestService
    {
        public async Task<ResultService> RejectTattooRequestByArtistAsync(int id, string userId)
        {
            await using var transaction=await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var tattooArtist=await context.TattooArtists.FirstOrDefaultAsync(a=>a.UserId==userId);
            if(tattooArtist==null)return ResultService.Fail("Tattoo artist profile was not found.");
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{id}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");
            var tattooRequest=await context.TattooRequests.Include(r=>r.ArtistResponse).FirstOrDefaultAsync(r=>r.Id==id);
            if(tattooRequest==null||tattooRequest.TattooArtistId!=tattooArtist.Id)return ResultService.Fail("Tattoo request was not found.");
            if(tattooRequest.Status is not (RequestStatus.Submitted or RequestStatus.UnderReview)||tattooRequest.ArtistResponse!=null)return ResultService.Fail("Only a pre-approval tattoo request can be rejected. Use cancellation after approval.");
            var transition=TattooRequestStateMachine.Transition(tattooRequest,RequestStatus.Rejected);if(!transition.Success)return transition;
            tattooRequest.RemainingSessionsToBook=0;await context.SaveChangesAsync();await transaction.CommitAsync();return ResultService.Ok();
        }

        public async Task<ResultService> MarkUnderReviewAsync(int id, string userId)
        {
            await using var transaction=await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var artist=await context.TattooArtists.FirstOrDefaultAsync(a=>a.UserId==userId);if(artist==null)return ResultService.Fail("Tattoo artist profile was not found.");
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{id}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");
            var request=await context.TattooRequests.FirstOrDefaultAsync(r=>r.Id==id&&r.TattooArtistId==artist.Id);if(request==null)return ResultService.Fail("Tattoo request was not found.");
            var transition=TattooRequestStateMachine.Transition(request,RequestStatus.UnderReview);if(!transition.Success)return transition;
            await context.SaveChangesAsync();await transaction.CommitAsync();return ResultService.Ok();
        }

        public async Task<ResultService> CancelTattooRequestAsync(int id, string userId, string? reason)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var requestArtistId=await context.TattooRequests.Where(r=>r.Id==id).Select(r=>(int?)r.TattooArtistId).FirstOrDefaultAsync();
            if(requestArtistId==null)return ResultService.Fail("Tattoo request was not found.");
            await BookingConcurrency.AcquireArtistLockAsync(context,requestArtistId.Value);
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{id}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");
            var request = await context.TattooRequests
                .Include(r => r.Consultation)
                .Include(r => r.TattooSessions)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return ResultService.Fail("Tattoo request was not found.");

            var clientId = await context.Clients.Where(c => c.UserId == userId).Select(c => (int?)c.Id).FirstOrDefaultAsync();
            var artistId = await context.TattooArtists.Where(a => a.UserId == userId).Select(a => (int?)a.Id).FirstOrDefaultAsync();
            var isClient = clientId == request.ClientId;
            var isArtist = artistId == request.TattooArtistId;
            if (!isClient && !isArtist) return ResultService.Fail("Tattoo request was not found.");
            if (request.Status is RequestStatus.Submitted or RequestStatus.UnderReview)
                return ResultService.Fail("Pre-approval requests must be rejected instead of cancelled.");
            if (request.Status is RequestStatus.Completed or RequestStatus.Rejected or RequestStatus.Cancelled)
                return ResultService.Fail("This tattoo request cannot be cancelled from its current status.");

            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (isClient && !isArtist)
            {
                var cutoff = now.AddHours(24);
                var blockedByConsultation = request.Consultation is { IsCancelled: false } c && c.StartTime > now && c.StartTime <= cutoff;
                var blockedBySession = request.TattooSessions.Any(s => !s.IsCancelled && s.StartTime > now && s.StartTime <= cutoff);
                if (blockedByConsultation || blockedBySession)
                    return ResultService.Fail("Clients can cancel a tattoo request only when no appointment starts within the next 24 hours.");
            }

            var transition = TattooRequestStateMachine.Transition(request, RequestStatus.Cancelled);
            if (!transition.Success) return transition;
            request.CancelledAt = now;
            request.CancelledByUserId = userId;
            request.CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()[..Math.Min(reason.Trim().Length, 500)];

            if (request.Consultation is { IsCancelled: false, IsCompleted: false } consultation && consultation.StartTime > now)
            {
                consultation.IsCancelled = true;
                consultation.CancelledAt = now;
                consultation.CancelledByUserId = userId;
                consultation.CancellationReason = "Cancelled with tattoo request.";
            }
            foreach (var session in request.TattooSessions.Where(s => !s.IsCancelled && s.StartTime > now))
            {
                session.IsCancelled = true;
                session.CancelledAt = now;
                session.CancelledByUserId = userId;
                session.CancellationReason = "Cancelled with tattoo request.";
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService<ICollection<GetTattooRequestDto>>> GetAllTattooRequestsAsync()
        {
            var tattooRequests = await context.TattooRequests
                .Include(r => r.Client)
                .Include(r => r.TattooArtist)
                    .ThenInclude(a => a.Studio)
                .Include(r => r.Images)
                .Include(r => r.TattooSessions)
                .Include(r => r.ArtistResponse)
                .Include(r => r.Consultation)
                .ToListAsync();

            var result = tattooRequests
                .Select(MapToGetTattooRequestDto)
                .ToList();

            return ResultService<ICollection<GetTattooRequestDto>>.Ok(result);
        }

        public async Task<ResultService<GetTattooRequestDto>> GetTattooRequestByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist)
        {
            var tattooRequest = await context.TattooRequests
                .Include(r => r.Client)
                .Include(r => r.TattooArtist)
                    .ThenInclude(a => a.Studio)
                .Include(r => r.Images)
                .Include(r => r.TattooSessions)
                .Include(r => r.ArtistResponse)
                .Include(r => r.Consultation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (tattooRequest == null)
            {
                return ResultService<GetTattooRequestDto>.Fail("Tattoo request was not found.");
            }

            if (isAdmin)
            {
                return ResultService<GetTattooRequestDto>.Ok(
                    MapToGetTattooRequestDto(tattooRequest));
            }

            if (isClient)
            {
                var client = await context.Clients
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (client != null &&
                    tattooRequest.ClientId == client.Id)
                {
                    return ResultService<GetTattooRequestDto>.Ok(
                        MapToGetTattooRequestDto(tattooRequest));
                }
            }

            if (isArtist)
            {
                var tattooArtist = await context.TattooArtists
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (tattooArtist != null &&
                    tattooRequest.TattooArtistId == tattooArtist.Id)
                {
                    return ResultService<GetTattooRequestDto>.Ok(
                        MapToGetTattooRequestDto(tattooRequest));
                }
            }

            return ResultService<GetTattooRequestDto>.Fail(
                "You do not have permission to view this tattoo request.");
        }

        public async Task<ResultService<ICollection<GetTattooRequestDto>>> GetMyTattooRequestsAsync(
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService<ICollection<GetTattooRequestDto>>.Fail(
                    "Client profile was not found.");
            }

            var tattooRequests = await context.TattooRequests
                .Include(r => r.Client)
                .Include(r => r.TattooArtist)
                    .ThenInclude(a => a.Studio)
                .Include(r => r.Images)
                .Include(r => r.TattooSessions)
                .Include(r => r.ArtistResponse)
                .Include(r => r.Consultation)
                .Where(r => r.ClientId == client.Id)
                .OrderByDescending(r => r.CreatedOn)
                .ToListAsync();

            var result = tattooRequests
                .Select(MapToGetTattooRequestDto)
                .ToList();

            return ResultService<ICollection<GetTattooRequestDto>>.Ok(result);
        }

        public async Task<ResultService<ICollection<GetTattooRequestDto>>> GetMyArtistTattooRequestsAsync(
            string userId,
            RequestStatus? status)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService<ICollection<GetTattooRequestDto>>.Fail(
                    "Tattoo artist profile was not found.");
            }

            var query = context.TattooRequests
                .Include(r => r.Client)
                .Include(r => r.TattooArtist)
                    .ThenInclude(a => a.Studio)
                .Include(r => r.Images)
                .Include(r => r.TattooSessions)
                .Include(r => r.ArtistResponse)
                .Include(r => r.Consultation)
                .Where(r => r.TattooArtistId == tattooArtist.Id)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var tattooRequests = await query
                .OrderByDescending(r => r.CreatedOn)
                .ToListAsync();

            var result = tattooRequests
                .Select(MapToGetTattooRequestDto)
                .ToList();

            return ResultService<ICollection<GetTattooRequestDto>>.Ok(result);
        }

        public async Task<ResultService> CreateTattooRequestAsync(
            CreateTattooRequestDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile was not found.");
            }

            var tattooArtist = await context.TattooArtists
                .Include(a => a.Subscription)
                .FirstOrDefaultAsync(a => a.Id == dto.TattooArtistId);

            if (tattooArtist == null || tattooArtist.StudioId == null)
            {
                return ResultService.Fail("Tattoo artist was not found or is not currently part of a studio.");
            }

            if (tattooArtist.Subscription == null || !SubscriptionEntitlementRules.HasAccess(tattooArtist.Subscription, timeProvider.GetUtcNow().UtcDateTime))
                return ResultService.Fail("Tattoo artist is not currently available for new requests.");

            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                return ResultService.Fail("Tattoo request description is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Placement))
            {
                return ResultService.Fail("Tattoo placement is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.TattooStyle))
            {
                return ResultService.Fail("Tattoo style is required.");
            }

            if (dto.Images.Count > 0)
            {
                return ResultService.Fail("External image URLs are no longer accepted. Use POST /api/TattooRequest/with-images with multipart uploads.");
            }

            TattooRequest tattooRequest = new()
            {
                ClientId = client.Id,
                TattooArtistId = tattooArtist.Id,
                Description = dto.Description,
                Placement = dto.Placement,
                TattooStyle = dto.TattooStyle.Trim(),
                Status = RequestStatus.Submitted,
                CreatedOn = timeProvider.GetUtcNow().UtcDateTime
            };

            context.TattooRequests.Add(tattooRequest);

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }


        public async Task<ResultService<int>> CreateTattooRequestWithImagesAsync(
            CreateTattooRequestWithImagesDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService<int>.Fail("Client profile was not found.");
            }

            var tattooArtist = await context.TattooArtists
                .Include(a => a.Subscription)
                .FirstOrDefaultAsync(a => a.Id == dto.TattooArtistId);

            if (tattooArtist == null || tattooArtist.StudioId == null)
            {
                return ResultService<int>.Fail("Tattoo artist was not found or is not currently part of a studio.");
            }

            if (tattooArtist.Subscription == null || !SubscriptionEntitlementRules.HasAccess(tattooArtist.Subscription, timeProvider.GetUtcNow().UtcDateTime))
                return ResultService<int>.Fail("Tattoo artist is not currently available for new requests.");

            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                return ResultService<int>.Fail("Tattoo request description is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Placement))
            {
                return ResultService<int>.Fail("Tattoo placement is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.TattooStyle))
            {
                return ResultService<int>.Fail("Tattoo style is required.");
            }

            var tattooRequest = new TattooRequest
            {
                ClientId = client.Id,
                TattooArtistId = tattooArtist.Id,
                Description = dto.Description.Trim(),
                Placement = dto.Placement.Trim(),
                TattooStyle = dto.TattooStyle.Trim(),
                Status = RequestStatus.Submitted,
                CreatedOn = timeProvider.GetUtcNow().UtcDateTime
            };

            var storedKeys = new List<string>();
            if (dto.Images != null)
            {
                foreach (var image in dto.Images)
                {
                    var sanitized = await imageSanitizer.SanitizeAsync(image, 5 * 1024 * 1024, 30_000_000);
                    if (!sanitized.Success)
                    {
                        foreach (var storedKey in storedKeys) await storage.DeleteAsync(storedKey);
                        return ResultService<int>.Fail(sanitized.ErrorMessage!);
                    }
                    var key = await storage.SaveAsync(sanitized.Data!.Bytes, "tattoo-request-images", sanitized.Data.Extension, StoredFileVisibility.Private);
                    storedKeys.Add(key);
                    tattooRequest.Images.Add(new TattooReferenceImage { ImageUrl = key });
                }
            }

            try
            {
                context.TattooRequests.Add(tattooRequest);
                await context.SaveChangesAsync();
                return ResultService<int>.Ok(tattooRequest.Id);
            }
            catch
            {
                foreach (var key in storedKeys) await storage.DeleteAsync(key);
                throw;
            }
        }

        public async Task<ResultService<BookingAvailabilityDto>> GetBookingAvailabilityAsync(
            int tattooRequestId,
            string bookingType,
            string userId,
            DateTime? startDate = null,
            int days = 14)
        {
            var client = await context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
                return ResultService<BookingAvailabilityDto>.Fail("Client profile was not found.");

            var tattooRequest = await context.TattooRequests
                .AsNoTracking()
                .Include(r => r.TattooArtist).ThenInclude(a => a.Schedules)
                .Include(r => r.TattooArtist).ThenInclude(a => a.Studio)
                .Include(r => r.TattooSessions)
                .Include(r => r.ArtistResponse)
                .Include(r => r.Consultation)
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);

            if (tattooRequest == null || tattooRequest.ClientId != client.Id)
                return ResultService<BookingAvailabilityDto>.Fail("Tattoo request was not found.");

            var normalizedType = bookingType.Trim().ToLowerInvariant();
            ScheduleType scheduleType;
            int durationMinutes;

            if (normalizedType == "consultation")
            {
                if (tattooRequest.Status != RequestStatus.Approved ||
                    tattooRequest.ArtistResponse?.WorkflowPath != ArtistResponseWorkflowPath.ConsultationFirst ||
                    tattooRequest.Consultation != null)
                    return ResultService<BookingAvailabilityDto>.Fail("Consultation cannot be booked for the current request state.");
                scheduleType = ScheduleType.Consultation;
                durationMinutes = tattooRequest.TattooArtist.ConsultationDurationMinutes;
            }
            else if (normalizedType is "session" or "tattoo-session")
            {
                var approvedDirect = tattooRequest.Status == RequestStatus.Approved &&
                                     tattooRequest.ArtistResponse?.WorkflowPath == ArtistResponseWorkflowPath.DirectToSessions;
                if (!approvedDirect && tattooRequest.Status is not (RequestStatus.ConsultationCompleted or RequestStatus.TattooBooked or RequestStatus.InProgress))
                    return ResultService<BookingAvailabilityDto>.Fail("Tattoo session cannot be booked for the current request state.");
                var existingSessionsCount = tattooRequest.TattooSessions?.Count ?? 0;
                if (tattooRequest.DurationHoursForSession == null || existingSessionsCount >= tattooRequest.DurationHoursForSession.Count)
                    return ResultService<BookingAvailabilityDto>.Fail("No remaining session duration was found.");
                scheduleType = ScheduleType.TattooSession;
                durationMinutes = tattooRequest.DurationHoursForSession[existingSessionsCount] * 60;
            }
            else
            {
                return ResultService<BookingAvailabilityDto>.Fail("Unknown booking type.");
            }

            if (durationMinutes <= 0)
                return ResultService<BookingAvailabilityDto>.Fail("Booking duration is invalid.");

            var zoneId = string.IsNullOrWhiteSpace(tattooRequest.TattooArtist.TimeZoneId)
                ? TimeZoneSupport.DefaultTimeZoneId
                : tattooRequest.TattooArtist.TimeZoneId;
            var zoneResult = TimeZoneSupport.Get(zoneId);
            if (!zoneResult.Success)
                return ResultService<BookingAvailabilityDto>.Fail(zoneResult.ErrorMessage!);
            var zone = zoneResult.Data!;

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone).Date;
            var periodStart = (startDate ?? todayLocal).Date;
            if (periodStart < todayLocal) periodStart = todayLocal;
            if (periodStart > todayLocal.AddYears(1))
                return ResultService<BookingAvailabilityDto>.Fail("Availability can be viewed up to one year in advance.");

            var periodDays = Math.Clamp(days, 1, 31);
            var periodEndLocal = periodStart.AddDays(periodDays);
            var rangeStartResult = TimeZoneSupport.ToUtc(periodStart, zoneId);
            var rangeEndResult = TimeZoneSupport.ToUtc(periodEndLocal, zoneId);
            if (!rangeStartResult.Success || !rangeEndResult.Success)
                return ResultService<BookingAvailabilityDto>.Fail("The requested availability period crosses an invalid timezone boundary.");
            var rangeStartUtc = rangeStartResult.Data;
            var rangeEndUtc = rangeEndResult.Data;

            // Load all conflicts for the requested period once; slot generation below is in-memory.
            var consultations = await context.Consultations.AsNoTracking()
                .Where(c => !c.IsCancelled && c.TattooRequest.TattooArtistId == tattooRequest.TattooArtistId &&
                            c.StartTime < rangeEndUtc && c.EndTime > rangeStartUtc)
                .Select(c => new { c.StartTime, c.EndTime })
                .ToListAsync();
            var sessions = await context.TattooSessions.AsNoTracking()
                .Where(t => !t.IsCancelled && t.TattooRequest.TattooArtistId == tattooRequest.TattooArtistId &&
                            t.StartTime < rangeEndUtc && t.EndTime > rangeStartUtc)
                .Select(t => new { t.StartTime, t.EndTime })
                .ToListAsync();
            var unavailable = await context.ArtistUnavailableDates.AsNoTracking()
                .Where(u => u.TattooArtistId == tattooRequest.TattooArtistId &&
                            u.StartDateTime < rangeEndUtc && u.EndDateTime > rangeStartUtc)
                .Select(u => new { StartTime = u.StartDateTime, EndTime = u.EndDateTime })
                .ToListAsync();

            bool Conflicts(DateTime startUtc, DateTime endUtc) =>
                consultations.Any(x => startUtc < x.EndTime && endUtc > x.StartTime) ||
                sessions.Any(x => startUtc < x.EndTime && endUtc > x.StartTime) ||
                unavailable.Any(x => startUtc < x.EndTime && endUtc > x.StartTime);

            var result = new BookingAvailabilityDto
            {
                TattooRequestId = tattooRequest.Id,
                BookingType = normalizedType,
                DurationMinutes = durationMinutes,
                TimeZoneId = zoneId
            };
            var schedules = tattooRequest.TattooArtist.Schedules.Where(s => s.ScheduleType == scheduleType).ToList();
            var bookingDuration = TimeSpan.FromMinutes(durationMinutes);
            var slotIncrement = TimeSpan.FromMinutes(30);

            for (var i = 0; i < periodDays; i++)
            {
                var day = periodStart.AddDays(i);
                var daySchedules = schedules.Where(s => s.DayOfWeek == day.DayOfWeek).ToList();
                var dayDto = new BookingAvailabilityDayDto
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    IsAvailableDay = daySchedules.Count > 0,
                    Reason = daySchedules.Count > 0 ? null : "Outside the artist schedule."
                };

                foreach (var schedule in daySchedules)
                {
                    var cursorLocal = DateTime.SpecifyKind(day.Add(schedule.StartTime.ToTimeSpan()), DateTimeKind.Unspecified);
                    var scheduleEndLocal = DateTime.SpecifyKind(day.Add(schedule.EndTime.ToTimeSpan()), DateTimeKind.Unspecified);
                    while (cursorLocal.Add(bookingDuration) <= scheduleEndLocal)
                    {
                        var endLocal = cursorLocal.Add(bookingDuration);
                        var startUtcResult = TimeZoneSupport.ToUtc(cursorLocal, zoneId);
                        var endUtcResult = TimeZoneSupport.ToUtc(endLocal, zoneId);
                        if (startUtcResult.Success && endUtcResult.Success)
                        {
                            var startUtc = startUtcResult.Data;
                            var endUtc = endUtcResult.Data;
                            if (startUtc > nowUtc && !Conflicts(startUtc, endUtc))
                            {
                                dayDto.Slots.Add(new BookingAvailabilitySlotDto
                                {
                                    StartTime = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
                                    EndTime = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc),
                                    Label = cursorLocal.ToString("HH:mm")
                                });
                            }
                        }
                        cursorLocal = cursorLocal.Add(slotIncrement);
                    }
                }

                if (dayDto.IsAvailableDay && dayDto.Slots.Count == 0)
                    dayDto.Reason = "No free slots for this day.";
                result.Days.Add(dayDto);
            }

            return ResultService<BookingAvailabilityDto>.Ok(result);
        }

        public async Task<ResultService> UpdateTattooRequestAsync(
            int id,
            UpdateTattooRequestDto dto,
            string userId)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile was not found.");
            }

            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:TattooRequest:{id}","request_concurrency_conflict","The tattoo request is being updated by another operation. Refresh and retry.");
            var tattooRequest = await context.TattooRequests
                .Include(r => r.Client)
                .Include(r => r.TattooArtist)
                    .ThenInclude(a => a.Studio)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (tattooRequest == null)
            {
                return ResultService.Fail("Tattoo request was not found.");
            }

            if (tattooRequest.ClientId != client.Id)
            {
                return ResultService.Fail("You can update only your own tattoo requests.");
            }

            if (tattooRequest.Status != RequestStatus.Submitted)
            {
                return ResultService.Fail(
                    "Tattoo request can be updated only while it is submitted.");
            }

            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                return ResultService.Fail("Tattoo request description is required.");
            }

            if (dto.Images.Count > 0)
                return ResultService.Fail("Reference images cannot be replaced by URL. Use the multipart media flow.");

            tattooRequest.Description = dto.Description.Trim();

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ResultService.Ok();
        }


        private async Task<bool> HasBookingConflictAsync(int tattooArtistId, DateTime startTime, DateTime endTime)
        {
            var hasConsultationConflict = await context.Consultations.AnyAsync(c =>
                !c.IsCancelled && c.TattooRequest.TattooArtistId == tattooArtistId &&
                startTime < c.EndTime &&
                endTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return true;
            }

            var hasSessionConflict = await context.TattooSessions.AnyAsync(s =>
                !s.IsCancelled && s.TattooRequest.TattooArtistId == tattooArtistId &&
                startTime < s.EndTime &&
                endTime > s.StartTime);

            if (hasSessionConflict)
            {
                return true;
            }

            return await context.ArtistUnavailableDates.AnyAsync(u =>
                u.TattooArtistId == tattooArtistId &&
                startTime < u.EndDateTime &&
                endTime > u.StartDateTime);
        }

        private GetTattooRequestDto MapToGetTattooRequestDto(
            TattooRequest tattooRequest)
        {
            return new GetTattooRequestDto
            {
                Id = tattooRequest.Id,
                Description = tattooRequest.Description,
                Placement = tattooRequest.Placement,
                TattooStyle = tattooRequest.TattooStyle,
                CreatedOn = tattooRequest.CreatedOn,
                ClientId = tattooRequest.ClientId,
                TattooArtistId = tattooRequest.TattooArtistId,
                RemainingSessionsToBook = tattooRequest.RemainingSessionsToBook,
                PriceForSession = tattooRequest.PriceForSession?.ToList(),
                DurationHoursForSession = tattooRequest.DurationHoursForSession?.ToList(),
                ClientName = tattooRequest.Client == null
                    ? null
                    : $"{tattooRequest.Client.FirstName} {tattooRequest.Client.LastName}",
                ClientEmail = tattooRequest.Client?.Email,
                ClientPhoneNumber = tattooRequest.Client?.PhoneNumber,
                ClientCity = tattooRequest.Client?.City,
                ClientCountry = tattooRequest.Client?.Country,
                TattooArtistName = tattooRequest.TattooArtist == null
                    ? null
                    : $"{tattooRequest.TattooArtist.FirstName} {tattooRequest.TattooArtist.LastName}",
                StudioName = tattooRequest.TattooArtist?.Studio?.Name,
                Status = tattooRequest.Status,

                UpcomingConsultationStartTime = tattooRequest.Consultation != null &&
                                                tattooRequest.Consultation.StartTime >= timeProvider.GetUtcNow().UtcDateTime
                    ? tattooRequest.Consultation.StartTime
                    : null,

                UpcomingTattooSessionStartTime = tattooRequest.TattooSessions != null
                    ? tattooRequest.TattooSessions
                        .Where(s => s.StartTime >= timeProvider.GetUtcNow().UtcDateTime)
                        .OrderBy(s => s.StartTime)
                        .Select(s => (DateTime?)s.StartTime)
                        .FirstOrDefault()
                    : null,

                Images = tattooRequest.Images.Select(i => new TattooReferenceImageDto
                {
                    ImageUrl = privateMedia.CreateReadUrl(i.ImageUrl)
                }).ToList(),

                TattooSessions = tattooRequest.TattooSessions == null ||
                                 !tattooRequest.TattooSessions.Any()
                    ? null
                    : tattooRequest.TattooSessions.Select(s => new TattooSessionDto
                    {
                        Id = s.Id,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        DurationHours = s.DurationHours,
                        PriceForTheSession = s.PriceForTheSession
                    }).ToList(),

                ArtistResponse = tattooRequest.ArtistResponse == null
                    ? null
                    : new ArtistResponseDto
                    {
                        EstimatedPrice = tattooRequest.ArtistResponse.EstimatedPrice,
                        EstimatedHours = tattooRequest.ArtistResponse.EstimatedHours,
                        ResponseMessage = tattooRequest.ArtistResponse.ResponseMessage,
                        WorkflowPath = tattooRequest.ArtistResponse.WorkflowPath,
                        CreatedOn = tattooRequest.ArtistResponse.CreatedOn
                    },

                Consultation = tattooRequest.Consultation == null
                    ? null
                    : new ConsultationDto
                    {
                        Id = tattooRequest.Consultation.Id,
                        StartTime = tattooRequest.Consultation.StartTime,
                        EndTime = tattooRequest.Consultation.EndTime,
                        Notes = tattooRequest.Consultation.Notes
                    }
            };
        }
    }
}
