using Microsoft.AspNetCore.Hosting;
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
    public class TattooRequestService(TattooDbContext context, IWebHostEnvironment environment)
        : ITattooRequestService
    {
        public async Task<ResultService<ICollection<GetTattooRequestDto>>> GetAllTattooRequestsAsync()
        {
            var tattooRequests = await context.TattooRequests
                .Include(r => r.Client)
                .Include(r => r.TattooArtist)
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
                .FirstOrDefaultAsync(a => a.Id == dto.TattooArtistId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist was not found.");
            }

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

            if (dto.Images.Any(i => string.IsNullOrWhiteSpace(i.ImageUrl)))
            {
                return ResultService.Fail("Every reference image must have a valid image URL.");
            }

            TattooRequest tattooRequest = new()
            {
                ClientId = client.Id,
                TattooArtistId = tattooArtist.Id,
                Description = dto.Description,
                Placement = dto.Placement,
                TattooStyle = dto.TattooStyle.Trim(),
                Status = RequestStatus.Submitted,
                CreatedOn = DateTime.UtcNow,
                Images = dto.Images.Select(i => new TattooReferenceImage
                {
                    ImageUrl = i.ImageUrl
                }).ToList()
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
                .FirstOrDefaultAsync(a => a.Id == dto.TattooArtistId);

            if (tattooArtist == null)
            {
                return ResultService<int>.Fail("Tattoo artist was not found.");
            }

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
                CreatedOn = DateTime.UtcNow
            };

            if (dto.Images != null)
            {
                foreach (var image in dto.Images)
                {
                    var validation = ValidateImage(image);
                    if (!validation.Success)
                    {
                        return ResultService<int>.Fail(validation.ErrorMessage!);
                    }

                    tattooRequest.Images.Add(new TattooReferenceImage
                    {
                        ImageUrl = await SaveImageAsync(image, "tattoo-request-images")
                    });
                }
            }

            context.TattooRequests.Add(tattooRequest);
            await context.SaveChangesAsync();

            return ResultService<int>.Ok(tattooRequest.Id);
        }

        public async Task<ResultService<BookingAvailabilityDto>> GetBookingAvailabilityAsync(
            int tattooRequestId,
            string bookingType,
            string userId)
        {
            var client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
            {
                return ResultService<BookingAvailabilityDto>.Fail("Client profile was not found.");
            }

            var tattooRequest = await context.TattooRequests
                .Include(r => r.TattooArtist)
                    .ThenInclude(a => a.Schedules)
                .Include(r => r.TattooSessions)
                .Include(r => r.ArtistResponse)
                .Include(r => r.Consultation)
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);

            if (tattooRequest == null)
            {
                return ResultService<BookingAvailabilityDto>.Fail("Tattoo request was not found.");
            }

            if (tattooRequest.ClientId != client.Id)
            {
                return ResultService<BookingAvailabilityDto>.Fail("You can view availability only for your own tattoo requests.");
            }

            var normalizedType = bookingType.Trim().ToLowerInvariant();
            ScheduleType scheduleType;
            int durationMinutes;

            if (normalizedType == "consultation")
            {
                if (tattooRequest.Status != RequestStatus.WaitingForConsultation || tattooRequest.ArtistResponse == null || tattooRequest.Consultation != null)
                {
                    return ResultService<BookingAvailabilityDto>.Fail("Consultation cannot be booked for the current request state.");
                }

                scheduleType = ScheduleType.Consultation;
                durationMinutes = tattooRequest.TattooArtist.ConsultationDurationMinutes;
            }
            else if (normalizedType == "session" || normalizedType == "tattoo-session")
            {
                if (tattooRequest.Status != RequestStatus.ConsultationCompleted &&
                    tattooRequest.Status != RequestStatus.TattooBooked &&
                    tattooRequest.Status != RequestStatus.InProgress)
                {
                    return ResultService<BookingAvailabilityDto>.Fail("Tattoo session cannot be booked for the current request state.");
                }

                var existingSessionsCount = tattooRequest.TattooSessions?.Count ?? 0;
                if (tattooRequest.DurationHoursForSession == null || existingSessionsCount >= tattooRequest.DurationHoursForSession.Count)
                {
                    return ResultService<BookingAvailabilityDto>.Fail("No remaining session duration was found.");
                }

                scheduleType = ScheduleType.TattooSession;
                durationMinutes = tattooRequest.DurationHoursForSession[existingSessionsCount] * 60;
            }
            else
            {
                return ResultService<BookingAvailabilityDto>.Fail("Unknown booking type.");
            }

            if (durationMinutes <= 0)
            {
                return ResultService<BookingAvailabilityDto>.Fail("Booking duration is invalid.");
            }

            var result = new BookingAvailabilityDto
            {
                TattooRequestId = tattooRequest.Id,
                BookingType = normalizedType,
                DurationMinutes = durationMinutes
            };

            var today = DateTime.Today;
            var schedules = tattooRequest.TattooArtist.Schedules
                .Where(s => s.ScheduleType == scheduleType)
                .ToList();

            for (var i = 0; i < 30; i++)
            {
                var day = today.AddDays(i);
                var daySchedules = schedules.Where(s => s.DayOfWeek == day.DayOfWeek).ToList();
                var dayDto = new BookingAvailabilityDayDto
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    IsAvailableDay = daySchedules.Any(),
                    Reason = daySchedules.Any() ? null : "Outside the artist schedule."
                };

                foreach (var schedule in daySchedules)
                {
                    var cursor = day.Add(schedule.StartTime.ToTimeSpan());
                    var scheduleEnd = day.Add(schedule.EndTime.ToTimeSpan());
                    var bookingDuration = TimeSpan.FromMinutes(durationMinutes);
                    var slotIncrement = TimeSpan.FromMinutes(30);

                    while (cursor.Add(bookingDuration) <= scheduleEnd)
                    {
                        var end = cursor.Add(bookingDuration);

                        if (cursor > DateTime.Now && !await HasBookingConflictAsync(tattooRequest.TattooArtistId, cursor, end))
                        {
                            dayDto.Slots.Add(new BookingAvailabilitySlotDto
                            {
                                StartTime = cursor,
                                EndTime = end,
                                Label = cursor.ToString("HH:mm")
                            });
                        }

                        cursor = cursor.Add(slotIncrement);
                    }
                }

                if (dayDto.IsAvailableDay && !dayDto.Slots.Any())
                {
                    dayDto.Reason = "No free slots for this day.";
                }

                result.Days.Add(dayDto);
            }

            return ResultService<BookingAvailabilityDto>.Ok(result);
        }

        public async Task<ResultService> UpdateTattooRequestAsync(
            int id,
            UpdateTattooRequestDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile was not found.");
            }

            var tattooRequest = await context.TattooRequests
                .Include(r => r.Client)
                .Include(r => r.TattooArtist)
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

            if (dto.Images.Any(i => string.IsNullOrWhiteSpace(i.ImageUrl)))
            {
                return ResultService.Fail("Every reference image must have a valid image URL.");
            }

            tattooRequest.Description = dto.Description;

            tattooRequest.Images.Clear();

            foreach (var image in dto.Images)
            {
                tattooRequest.Images.Add(new TattooReferenceImage
                {
                    ImageUrl = image.ImageUrl
                });
            }

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }


        private async Task<bool> HasBookingConflictAsync(int tattooArtistId, DateTime startTime, DateTime endTime)
        {
            var hasConsultationConflict = await context.Consultations.AnyAsync(c =>
                c.TattooRequest.TattooArtistId == tattooArtistId &&
                startTime < c.EndTime &&
                endTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return true;
            }

            var hasSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.TattooRequest.TattooArtistId == tattooArtistId &&
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

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxImageSize = 5 * 1024 * 1024;

        private static ResultService ValidateImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return ResultService.Fail("Image is required.");
            }

            if (image.Length > MaxImageSize)
            {
                return ResultService.Fail("Image size cannot be bigger than 5MB.");
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
            {
                return ResultService.Fail("Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            return ResultService.Ok();
        }

        private async Task<string> SaveImageAsync(IFormFile image, string folderName)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var webRootPath = environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
            }

            var folderPath = Path.Combine(webRootPath, "uploads", folderName);
            Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folderPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            return $"/uploads/{folderName}/{fileName}";
        }

        private static GetTattooRequestDto MapToGetTattooRequestDto(
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
                StudioName = tattooRequest.TattooArtist == null
                    ? null
                    : tattooRequest.TattooArtist.StudioName,
                Status = tattooRequest.Status,

                UpcomingConsultationStartTime = tattooRequest.Consultation != null &&
                                                tattooRequest.Consultation.StartTime >= DateTime.UtcNow
                    ? tattooRequest.Consultation.StartTime
                    : null,

                UpcomingTattooSessionStartTime = tattooRequest.TattooSessions != null
                    ? tattooRequest.TattooSessions
                        .Where(s => s.StartTime >= DateTime.UtcNow)
                        .OrderBy(s => s.StartTime)
                        .Select(s => (DateTime?)s.StartTime)
                        .FirstOrDefault()
                    : null,

                Images = tattooRequest.Images.Select(i => new TattooReferenceImageDto
                {
                    ImageUrl = i.ImageUrl
                }).ToList(),

                TattooSessions = tattooRequest.TattooSessions == null ||
                                 !tattooRequest.TattooSessions.Any()
                    ? null
                    : tattooRequest.TattooSessions.Select(s => new TattooSessionDto
                    {
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
                        CreatedOn = tattooRequest.ArtistResponse.CreatedOn
                    },

                Consultation = tattooRequest.Consultation == null
                    ? null
                    : new ConsultationDto
                    {
                        StartTime = tattooRequest.Consultation.StartTime,
                        EndTime = tattooRequest.Consultation.EndTime,
                        Notes = tattooRequest.Consultation.Notes
                    }
            };
        }
    }
}