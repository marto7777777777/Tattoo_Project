using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ArtistResponceDTOs;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.DTOs.TattooArtistDTOs;
using Tattoo_Project.DTOs.TattooReferenceImageDTOs;
using Tattoo_Project.DTOs.TattooRequestDTOs;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class TattooArtistService(
        TattooDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        : ITattooArtistService
    {
        public async Task<ResultService<ICollection<GetTattooArtistDto>>> GetAllTattooArtistsAsync()
        {
            var artists = await context.TattooArtists
                .Include(a => a.User)
                .Include(a => a.Studio)
                .Include(a => a.Schedules)
                .Include(a => a.PortfolioImages)
                .Include(a => a.Requirements)
                .Include(a => a.Reviews)
                .Include(a => a.TattooRequests!)
                    .ThenInclude(r => r.Images)
                .Include(a => a.TattooRequests!)
                    .ThenInclude(r => r.TattooSessions)
                .Include(a => a.TattooRequests!)
                    .ThenInclude(r => r.ArtistResponse)
                .Include(a => a.TattooRequests!)
                    .ThenInclude(r => r.Consultation)
                .Where(a => a.StudioId != null)
                .ToListAsync();

            var result = artists
                .Select(MapToGetTattooArtistDto)
                .ToList();

            return ResultService<ICollection<GetTattooArtistDto>>.Ok(result);
        }

        public async Task<ResultService<GetTattooArtistDto>> GetTattooArtistByIdAsync(int id)
        {
            var artist = await context.TattooArtists
                .Include(a => a.User)
                .Include(a => a.Studio)
                .Include(a => a.Schedules)
                .Include(a => a.PortfolioImages)
                .Include(a => a.Reviews)
                .Include(a => a.Requirements)
                .Include(a => a.TattooRequests!)
                    .ThenInclude(r => r.Images)
                .Include(a => a.TattooRequests!)
                    .ThenInclude(r => r.TattooSessions)
                .Include(a => a.TattooRequests!)
                    .ThenInclude(r => r.ArtistResponse)
                .Include(a => a.TattooRequests!)
                    .ThenInclude(r => r.Consultation)
                .FirstOrDefaultAsync(a => a.Id == id && a.StudioId != null);

            if (artist == null)
            {
                return ResultService<GetTattooArtistDto>.Fail("Tattoo artist was not found.");
            }

            return ResultService<GetTattooArtistDto>.Ok(MapToGetTattooArtistDto(artist));
        }

        public async Task<ResultService<ICollection<GetTattooArtistDto>>> SearchTattooArtistsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetAllTattooArtistsAsync();
            }

            query = query.Trim().ToLower();

            var artists = await context.TattooArtists
                .Include(a => a.User)
                .Include(a => a.Studio)
                .Include(a => a.Schedules)
                .Include(a => a.PortfolioImages)
                .Include(a => a.Reviews)
                .Include(a => a.Requirements)
                .Where(a => a.StudioId != null && a.Studio != null)
                .Where(a =>
                    a.Studio!.Name.ToLower().Contains(query) ||
                    a.FirstName.ToLower().Contains(query) ||
                    a.LastName.ToLower().Contains(query) ||
                    a.Studio.City.ToLower().Contains(query) ||
                    a.Studio.Country.ToLower().Contains(query) ||
                    a.Studio.Address.ToLower().Contains(query))
                .OrderByDescending(a => a.Reviews.Any() ? a.Reviews.Average(r => r.Rating) : 0)
                .ThenByDescending(a => a.Reviews.Count)
                .ThenBy(a => a.Studio!.Name)
                .ToListAsync();

            return ResultService<ICollection<GetTattooArtistDto>>.Ok(artists.Select(MapToGetTattooArtistDto).ToList());
        }

        public async Task<ResultService> CreateTattooArtistProfileAsync(
            CreateTattooArtistDto dto,
            string userId)
        {
            if (await context.TattooArtists.AnyAsync(a => a.UserId == userId))
                return ResultService.Fail("Tattoo artist profile already exists.");

            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return ResultService.Fail("User was not found.");

            var validation = ValidateArtistProfile(dto.Description, dto.PhoneNumber, dto.ConsultationDurationMinutes,
                dto.RequiresDeposit, dto.DepositAmount, dto.Schedules, dto.Requirements);
            if (!validation.Success) return validation;

            Studio? selectedStudio = null;
            if (dto.StudioSetupMode == StudioSetupMode.CreateStudio)
            {
                if (dto.Studio == null) return ResultService.Fail("Studio information is required when creating a studio.");
                var studioValidation = StudioService.ValidateStudio(dto.Studio);
                if (!studioValidation.Success) return studioValidation;

                var duplicateStudio = await context.Studios.AnyAsync(s =>
                    s.Name.ToLower() == dto.Studio.Name.Trim().ToLower() &&
                    s.City.ToLower() == dto.Studio.City.Trim().ToLower() &&
                    s.Address.ToLower() == dto.Studio.Address.Trim().ToLower());
                if (duplicateStudio) return ResultService.Fail("A studio with the same name and address already exists.");
            }
            else if (dto.StudioSetupMode == StudioSetupMode.JoinStudio)
            {
                if (dto.JoinStudioId == null) return ResultService.Fail("Choose a studio to join.");
                selectedStudio = await context.Studios.FirstOrDefaultAsync(s => s.Id == dto.JoinStudioId.Value);
                if (selectedStudio == null) return ResultService.Fail("The selected studio was not found.");
                if (!selectedStudio.IsOpenForJoinRequests) return ResultService.Fail("The selected studio is not accepting new artists.");
            }
            else
            {
                return ResultService.Fail("Invalid studio setup option.");
            }

            var locationCity = dto.StudioSetupMode == StudioSetupMode.CreateStudio ? dto.Studio!.City.Trim() : selectedStudio!.City;
            var locationCountry = dto.StudioSetupMode == StudioSetupMode.CreateStudio ? dto.Studio!.Country.Trim() : selectedStudio!.Country;

            await using var transaction = await context.Database.BeginTransactionAsync();

            if (!await context.Clients.AnyAsync(c => c.UserId == userId))
            {
                context.Clients.Add(new Client
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!,
                    PhoneNumber = dto.PhoneNumber.Trim(),
                    UserId = user.Id,
                    City = locationCity,
                    Country = locationCountry
                });
            }

            var tattooArtist = new TattooArtist
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                Description = dto.Description.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                IsVerified = false,
                OffersOnlineConsultation = dto.OffersOnlineConsultation,
                RequiresDeposit = dto.RequiresDeposit,
                DepositAmount = dto.RequiresDeposit ? dto.DepositAmount : null,
                ConsultationDurationMinutes = dto.ConsultationDurationMinutes,
                UserId = user.Id,
                Requirements = (dto.Requirements ?? new List<TattooArtistRequirementsDto>())
                    .Where(r => !string.IsNullOrWhiteSpace(r.Description))
                    .Select(r => new ArtistRequirement { Description = r.Description.Trim() }).ToList(),
                PortfolioImages = (dto.PortfolioImages ?? new List<TattooArtistPortfolioImageDto>())
                    .Where(p => !string.IsNullOrWhiteSpace(p.ImageUrl))
                    .Select(p => new PortfolioImage { ImageUrl = p.ImageUrl.Trim() }).ToList(),
                Schedules = dto.Schedules.Select(s => new Schedule
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    ScheduleType = s.ScheduleType
                }).ToList()
            };

            context.TattooArtists.Add(tattooArtist);
            await context.SaveChangesAsync();

            if (dto.StudioSetupMode == StudioSetupMode.CreateStudio)
            {
                var studio = new Studio
                {
                    Name = dto.Studio!.Name.Trim(),
                    Description = dto.Studio.Description.Trim(),
                    Address = dto.Studio.Address.Trim(),
                    City = dto.Studio.City.Trim(),
                    Country = dto.Studio.Country.Trim(),
                    Latitude = dto.Studio.Latitude,
                    Longitude = dto.Studio.Longitude,
                    IsOpenForJoinRequests = true,
                    CreatedOn = DateTime.UtcNow,
                    OwnerArtistId = tattooArtist.Id
                };
                context.Studios.Add(studio);
                await context.SaveChangesAsync();

                tattooArtist.StudioId = studio.Id;
                tattooArtist.JoinedStudioOn = studio.CreatedOn;
            }
            else
            {
                context.StudioJoinRequests.Add(new StudioJoinRequest
                {
                    StudioId = selectedStudio!.Id,
                    TattooArtistId = tattooArtist.Id,
                    Status = StudioJoinRequestStatus.Pending,
                    CreatedOn = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await EnsureRoleExists(UserRoles.Client);
            await EnsureRoleExists(UserRoles.TattooArtist);
            if (!await userManager.IsInRoleAsync(user, UserRoles.Client))
                await userManager.AddToRoleAsync(user, UserRoles.Client);
            if (!await userManager.IsInRoleAsync(user, UserRoles.TattooArtist))
                await userManager.AddToRoleAsync(user, UserRoles.TattooArtist);

            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateTattooArtistProfileAsync(
            UpdateArtistDto dto,
            string userId)
        {
            var artist = await context.TattooArtists
                .Include(a => a.Requirements)
                .Include(a => a.PortfolioImages)
                .Include(a => a.Schedules)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (artist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            if (dto.RequiresDeposit &&
                (dto.DepositAmount == null || dto.DepositAmount <= 0))
            {
                return ResultService.Fail("Deposit amount must be greater than zero when deposit is required.");
            }

            if (dto.ConsultationDurationMinutes < 15)
            {
                return ResultService.Fail("Consultation duration must be at least 15 minutes.");
            }

            if (dto.ConsultationDurationMinutes > 180)
            {
                return ResultService.Fail("Consultation duration cannot be longer than 180 minutes.");
            }

            if (dto.Schedules == null || !dto.Schedules.Any())
            {
                return ResultService.Fail("At least one schedule is required.");
            }

            if (dto.Schedules.Any(s => s.StartTime >= s.EndTime))
            {
                return ResultService.Fail("Every schedule start time must be before end time.");
            }

            if (HasOverlappingSchedules(dto.Schedules))
            {
                return ResultService.Fail("Working hours cannot overlap on the same day, including consultation and tattoo session hours.");
            }

            if (!dto.Schedules.Any(s => s.ScheduleType == ScheduleType.Consultation))
            {
                return ResultService.Fail("At least one consultation schedule is required.");
            }

            if (!dto.Schedules.Any(s => s.ScheduleType == ScheduleType.TattooSession))
            {
                return ResultService.Fail("At least one tattoo session schedule is required.");
            }

            artist.Description = dto.Description.Trim();
            artist.PhoneNumber = dto.PhoneNumber.Trim();
            artist.OffersOnlineConsultation = dto.OffersOnlineConsultation;
            artist.RequiresDeposit = dto.RequiresDeposit;
            artist.DepositAmount = dto.RequiresDeposit ? dto.DepositAmount : null;
            artist.ConsultationDurationMinutes = dto.ConsultationDurationMinutes;

            artist.Requirements.Clear();
            artist.PortfolioImages.Clear();
            artist.Schedules.Clear();

            foreach (var requirement in dto.Requirements)
            {
                artist.Requirements.Add(new ArtistRequirement
                {
                    Description = requirement.Description
                });
            }

            foreach (var image in dto.PortfolioImages)
            {
                artist.PortfolioImages.Add(new PortfolioImage
                {
                    ImageUrl = image.ImageUrl
                });
            }

            foreach (var schedule in dto.Schedules)
            {
                artist.Schedules.Add(new Schedule
                {
                    DayOfWeek = schedule.DayOfWeek,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    ScheduleType = schedule.ScheduleType
                });
            }

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> DeleteTattooArtistAsync(int id)
        {
            var artist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.Id == id);

            if (artist == null)
            {
                return ResultService.Fail("Tattoo artist was not found.");
            }

            var ownedStudio = await context.Studios.FirstOrDefaultAsync(s => s.OwnerArtistId == artist.Id);
            if (ownedStudio != null)
            {
                var nextOwner = await context.TattooArtists
                    .Where(a => a.StudioId == ownedStudio.Id && a.Id != artist.Id)
                    .OrderBy(a => a.JoinedStudioOn ?? DateTime.MaxValue)
                    .ThenBy(a => a.Id)
                    .FirstOrDefaultAsync();

                if (nextOwner != null)
                {
                    ownedStudio.OwnerArtistId = nextOwner.Id;
                }
                else
                {
                    ownedStudio.OwnerArtistId = null;
                    artist.StudioId = null;
                    await context.SaveChangesAsync();
                    context.Studios.Remove(ownedStudio);
                    await context.SaveChangesAsync();
                }
            }

            context.TattooArtists.Remove(artist);
            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService<ICollection<GetTattooArtistDto>>> GetRecommendedTattooArtistsAsync(string userId)
        {
            var client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
                return ResultService<ICollection<GetTattooArtistDto>>.Fail("Client profile not found.");

            var clientCountry = client.Country.Trim().ToLower();
            var artistsQuery = context.TattooArtists
                .Include(a => a.User)
                .Include(a => a.Studio)
                .Include(a => a.Reviews)
                .Include(a => a.Schedules)
                .Include(a => a.PortfolioImages)
                .Include(a => a.Requirements)
                .Where(a => a.StudioId != null && a.Studio != null)
                .AsQueryable();

            if (await artistsQuery.AnyAsync(a => a.Studio!.Country.ToLower() == clientCountry))
                artistsQuery = artistsQuery.Where(a => a.Studio!.Country.ToLower() == clientCountry);

            var artists = await artistsQuery
                .OrderByDescending(a => a.IsVerified)
                .ThenByDescending(a => a.Reviews.Any() ? a.Reviews.Average(r => r.Rating) : 0)
                .ThenByDescending(a => a.Reviews.Count)
                .ThenBy(a => a.Studio!.Name)
                .ToListAsync();

            return ResultService<ICollection<GetTattooArtistDto>>.Ok(artists.Select(MapToGetTattooArtistDto).ToList());
        }

        private static ResultService ValidateArtistProfile(
            string? description,
            string? phoneNumber,
            int consultationDurationMinutes,
            bool requiresDeposit,
            decimal? depositAmount,
            ICollection<TattooArtistScheduleDto>? schedules,
            ICollection<TattooArtistRequirementsDto>? requirements)
        {
            if (string.IsNullOrWhiteSpace(description)) return ResultService.Fail("Artist description is required.");
            if (description.Trim().Length > 1200) return ResultService.Fail("Artist description cannot exceed 1200 characters.");
            if (string.IsNullOrWhiteSpace(phoneNumber)) return ResultService.Fail("Phone number is required.");
            if (phoneNumber.Trim().Length > 40) return ResultService.Fail("Phone number cannot exceed 40 characters.");
            if (consultationDurationMinutes < 15 || consultationDurationMinutes > 180)
                return ResultService.Fail("Consultation duration must be between 15 and 180 minutes.");
            if (requiresDeposit && (depositAmount == null || depositAmount <= 0))
                return ResultService.Fail("Deposit amount must be greater than zero when deposit is required.");
            if (depositAmount > 1_000_000) return ResultService.Fail("Deposit amount is too large.");
            if (schedules == null || schedules.Count == 0) return ResultService.Fail("At least one schedule is required.");
            if (schedules.Any(s => !Enum.IsDefined(typeof(DayOfWeek), s.DayOfWeek))) return ResultService.Fail("A schedule contains an invalid day of week.");
            if (schedules.Any(s => !Enum.IsDefined(typeof(ScheduleType), s.ScheduleType))) return ResultService.Fail("A schedule contains an invalid schedule type.");
            if (schedules.Any(s => s.StartTime >= s.EndTime)) return ResultService.Fail("Every schedule start time must be before end time.");
            if (!schedules.Any(s => s.ScheduleType == ScheduleType.Consultation)) return ResultService.Fail("At least one consultation schedule is required.");
            if (!schedules.Any(s => s.ScheduleType == ScheduleType.TattooSession)) return ResultService.Fail("At least one tattoo session schedule is required.");

            var duplicateSchedules = schedules
                .GroupBy(s => new { s.DayOfWeek, s.StartTime, s.EndTime, s.ScheduleType })
                .Any(g => g.Count() > 1);
            if (duplicateSchedules) return ResultService.Fail("Duplicate schedules are not allowed.");
            if (HasOverlappingSchedules(schedules)) return ResultService.Fail("Working hours cannot overlap on the same day, including consultation and tattoo session hours.");

            if (requirements != null)
            {
                if (requirements.Any(r => !string.IsNullOrWhiteSpace(r.Description) && r.Description.Trim().Length > 500))
                    return ResultService.Fail("A requirement cannot exceed 500 characters.");
                var duplicates = requirements.Where(r => !string.IsNullOrWhiteSpace(r.Description))
                    .GroupBy(r => r.Description.Trim(), StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1);
                if (duplicates) return ResultService.Fail("Duplicate requirements are not allowed.");
            }

            return ResultService.Ok();
        }

        private static bool HasOverlappingSchedules(IEnumerable<TattooArtistScheduleDto> schedules)
        {
            foreach (var dayGroup in schedules.GroupBy(s => s.DayOfWeek))
            {
                var ordered = dayGroup.OrderBy(s => s.StartTime).ThenBy(s => s.EndTime).ToList();
                for (var i = 1; i < ordered.Count; i++)
                {
                    if (ordered[i].StartTime < ordered[i - 1].EndTime) return true;
                }
            }
            return false;
        }

        private async Task EnsureRoleExists(string role)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
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
                IsVerified = artist.IsVerified,

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

                AverageRating = artist.Reviews.Any()
                    ? Math.Round(artist.Reviews.Average(r => r.Rating), 1)
                    : 0,

                ReviewCount = artist.Reviews.Count,

                ConsultationDurationMinutes = artist.ConsultationDurationMinutes,

                Schedules = artist.Schedules.Select(s => new TattooArtistScheduleDto
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    ScheduleType = s.ScheduleType
                }).ToList(),

                PortfolioImages = artist.PortfolioImages.Select(p => new TattooArtistPortfolioImageDto
                {
                    Id = p.Id,
                    ImageUrl = p.ImageUrl
                }).ToList(),

                Requirements = artist.Requirements.Select(r => new TattooArtistRequirementsDto
                {
                    Id = r.Id,
                    Description = r.Description
                }).ToList(),

                TattooRequests = artist.TattooRequests == null || !artist.TattooRequests.Any()
                    ? null
                    : artist.TattooRequests.Select(r => new TattooRequestDto
                    {
                        Description = r.Description,
                        Placement = r.Placement,
                        TattooStyle = r.TattooStyle,
                        CreatedOn = r.CreatedOn,
                        Status = r.Status,
                        ClientId = r.ClientId,
                        TattooArtistId = r.TattooArtistId,

                        Images = r.Images.Select(i => new TattooReferenceImageDto
                        {
                            ImageUrl = i.ImageUrl
                        }).ToList(),

                        TattooSessions = r.TattooSessions == null || !r.TattooSessions.Any()
                            ? null
                            : r.TattooSessions.Select(s => new TattooSessionDto
                            {
                                StartTime = s.StartTime,
                                EndTime = s.EndTime,
                                DurationHours = s.DurationHours,
                                PriceForTheSession = s.PriceForTheSession
                            }).ToList(),

                        ArtistResponse = r.ArtistResponse == null
                            ? null
                            : new ArtistResponseDto
                            {
                                CreatedOn = r.ArtistResponse.CreatedOn,
                                EstimatedHours = r.ArtistResponse.EstimatedHours,
                                EstimatedPrice = r.ArtistResponse.EstimatedPrice,
                                ResponseMessage = r.ArtistResponse.ResponseMessage
                            },

                        Consultation = r.Consultation == null
                            ? null
                            : new ConsultationDto
                            {
                                StartTime = r.Consultation.StartTime,
                                EndTime = r.Consultation.EndTime,
                                Notes = r.Consultation.Notes
                            }
                    }).ToList()
            };
        }
    }
}