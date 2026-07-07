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
                .ToListAsync();

            var result = artists
                .Select(MapToGetTattooArtistDto)
                .ToList();

            return ResultService<ICollection<GetTattooArtistDto>>.Ok(result);
        }

        public async Task<ResultService<GetTattooArtistDto>> GetTattooArtistByIdAsync(int id)
        {
            var artist = await context.TattooArtists
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
                .FirstOrDefaultAsync(a => a.Id == id);

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
                .Where(a =>
                    a.StudioName.ToLower().Contains(query) ||
                    a.FirstName.ToLower().Contains(query) ||
                    a.LastName.ToLower().Contains(query) ||
                    a.StudioCity.ToLower().Contains(query) ||
                    a.StudioCountry.ToLower().Contains(query) ||
                    a.StudioAddress.ToLower().Contains(query))
                .OrderByDescending(a => a.Reviews.Any()
                    ? a.Reviews.Average(r => r.Rating)
                    : 0)
                .ThenByDescending(a => a.Reviews.Count)
                .ThenBy(a => a.StudioName)
                .ToListAsync();

            var result = artists
                .Select(MapToGetTattooArtistDto)
                .ToList();

            return ResultService<ICollection<GetTattooArtistDto>>.Ok(result);
        }

        public async Task<ResultService> CreateTattooArtistProfileAsync(
            CreateTattooArtistDto dto,
            string userId)
        {
            var alreadyHasArtistProfile = await context.TattooArtists
                .AnyAsync(a => a.UserId == userId);

            if (alreadyHasArtistProfile)
            {
                return ResultService.Fail("Tattoo artist profile already exists.");
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return ResultService.Fail("User was not found.");
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

            if (!dto.Schedules.Any(s => s.ScheduleType == ScheduleType.Consultation))
            {
                return ResultService.Fail("At least one consultation schedule is required.");
            }

            if (!dto.Schedules.Any(s => s.ScheduleType == ScheduleType.TattooSession))
            {
                return ResultService.Fail("At least one tattoo session schedule is required.");
            }

            await EnsureRoleExists(UserRoles.Client);
            await EnsureRoleExists(UserRoles.TattooArtist);

            if (!await userManager.IsInRoleAsync(user, UserRoles.Client))
            {
                await userManager.AddToRoleAsync(user, UserRoles.Client);
            }

            if (!await userManager.IsInRoleAsync(user, UserRoles.TattooArtist))
            {
                await userManager.AddToRoleAsync(user, UserRoles.TattooArtist);
            }

            if (string.IsNullOrWhiteSpace(dto.StudioCity))
            {
                return ResultService.Fail("Studio city is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.StudioCountry))
            {
                return ResultService.Fail("Studio country is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.StudioAddress))
            {
                return ResultService.Fail("Studio address is required.");
            }

            var hasClientProfile = await context.Clients
                .AnyAsync(c => c.UserId == userId);

            if (!hasClientProfile)
            {
                Client client = new()
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!,
                    PhoneNumber = dto.PhoneNumber,
                    UserId = user.Id,
                    City = dto.StudioCity,
                    Country = dto.StudioCountry,
                };

                context.Clients.Add(client);
            }

            TattooArtist tattooArtist = new()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,

                StudioName = dto.StudioName,
                Description = dto.Description,
                StudioAddress = dto.StudioAddress,
                StudioCity = dto.StudioCity,
                StudioCountry = dto.StudioCountry,
                StudioLatitude = dto.StudioLatitude,
                StudioLongitude = dto.StudioLongitude,
                PhoneNumber = dto.PhoneNumber,

                IsVerified = false,

                OffersOnlineConsultation = dto.OffersOnlineConsultation,
                RequiresDeposit = dto.RequiresDeposit,
                DepositAmount = dto.RequiresDeposit ? dto.DepositAmount : null,

                ConsultationDurationMinutes = dto.ConsultationDurationMinutes,

                UserId = user.Id,

                Requirements = dto.Requirements.Select(r => new ArtistRequirement
                {
                    Description = r.Description
                }).ToList(),

                PortfolioImages = dto.PortfolioImages.Select(p => new PortfolioImage
                {
                    ImageUrl = p.ImageUrl
                }).ToList(),

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

            if (!dto.Schedules.Any(s => s.ScheduleType == ScheduleType.Consultation))
            {
                return ResultService.Fail("At least one consultation schedule is required.");
            }

            if (!dto.Schedules.Any(s => s.ScheduleType == ScheduleType.TattooSession))
            {
                return ResultService.Fail("At least one tattoo session schedule is required.");
            }

            artist.StudioName = dto.StudioName;
            artist.Description = dto.Description;
            artist.StudioAddress = dto.StudioAddress;
            artist.StudioCity = dto.StudioCity;
            artist.StudioCountry = dto.StudioCountry;
            artist.StudioLatitude = dto.StudioLatitude;
            artist.StudioLongitude = dto.StudioLongitude;
            artist.PhoneNumber = dto.PhoneNumber;
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

            context.TattooArtists.Remove(artist);

            await context.SaveChangesAsync();

            return ResultService.Ok();
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
                FirstName = artist.FirstName,
                LastName = artist.LastName,
                Email = artist.Email,

                StudioName = artist.StudioName,
                Description = artist.Description,
                StudioAddress = artist.StudioAddress,
                StudioCity = artist.StudioCity,
                StudioCountry = artist.StudioCountry,
                StudioLatitude = artist.StudioLatitude,
                StudioLongitude = artist.StudioLongitude,
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
                    ImageUrl = p.ImageUrl
                }).ToList(),

                Requirements = artist.Requirements.Select(r => new TattooArtistRequirementsDto
                {
                    Description = r.Description
                }).ToList(),

                TattooRequests = artist.TattooRequests == null || !artist.TattooRequests.Any()
                    ? null
                    : artist.TattooRequests.Select(r => new TattooRequestDto
                    {
                        Description = r.Description,
                        Placement = r.Placement,
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