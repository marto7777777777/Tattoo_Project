using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

namespace Tattoo_Project.Services
{
    public class TattooArtistService(
        TattooDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        : ITattooArtistService
    {
        public async Task<List<GetTattooArtistDto>> GetAllArtistsAsync()
        => context.TattooArtists == null || !context.TattooArtists.Any()? null
            :await context.TattooArtists.Select(c => new GetTattooArtistDto
        {
            FirstName = c.FirstName,
            LastName = c.LastName,
            DepositAmount = c.DepositAmount,
            Description = c.Description,
            Email = c.Email,
            OffersOnlineConsultation = c.OffersOnlineConsultation,
            PhoneNumber = c.PhoneNumber,
            RequiresDeposit = c.RequiresDeposit,
            StudioAddress = c.StudioAddress,
            StudioName = c.StudioName,
            Schedules = c.Schedules.Select(x => new TattooArtistScheduleDto
            {
                DayOfWeek = x.DayOfWeek,
                EndTime = x.EndTime,
                StartTime = x.StartTime,
            }).ToList(),
            PortfolioImages = c.PortfolioImages.Select(x => new TattooArtistPortfolioImageDto
            {
                ImageUrl = x.ImageUrl
            }).ToList(),
            Requirements = c.Requirements.Select(x => new TattooArtistRequirementsDto
            {
                Description = x.Description
            }).ToList(),
            TattooRequests = c.TattooRequests == null || !c.TattooRequests.Any() ? null
            : c.TattooRequests.Select(x => new TattooRequestDto
            {
                Status = x.Status,
                CreatedOn = x.CreatedOn,
                Description = x.Description,
                Placement = x.Placement,
                ClientId = x.ClientId,
                TattooArtistId = x.TattooArtistId,
                Images = x.Images.Select(f => new TattooReferenceImageDto
                {
                    ImageUrl = f.ImageUrl
                }).ToList(),
                TattooSessions = x.TattooSessions == null || !x.TattooSessions.Any() ? null
                : x.TattooSessions.Select(f => new TattooSessionDto
                {
                    StartTime = f.StartTime,
                    DurationHours = f.DurationHours,
                    EndTime = f.EndTime,
                    PriceForTheSession = f.PriceForTheSession
                }).ToList(),
                ArtistResponse = x.ArtistResponse == null? null
                : new ArtistResponseDto
                {
                    CreatedOn = x.ArtistResponse.CreatedOn,
                    EstimatedHours = x.ArtistResponse.EstimatedHours,
                    EstimatedPrice = x.ArtistResponse.EstimatedPrice,
                    ResponseMessage = x.ArtistResponse.ResponseMessage
                },
                Consultation = x.Consultation == null ? null
                : new ConsultationDto
                {
                    StartTime = x.Consultation.StartTime,
                    EndTime = x.Consultation.EndTime,
                    IsOnline = x.Consultation.IsOnline,
                    Notes = x.Consultation.Notes
                }
            }).ToList()

        }).ToListAsync();





        public async Task<GetTattooArtistDto> GetTattooArtistByIdAsync(int Id)
        {
            var artistFromData = await context.TattooArtists.Include(x => x.Schedules)
                .Include(x => x.PortfolioImages)
                .Include(x => x.Requirements)
                .Include(x => x.TattooRequests)
                .ThenInclude(x => x.Images)
                .Include(x => x.TattooRequests)
                .ThenInclude(x => x.TattooSessions)
                .Include(x => x.TattooRequests)
                .ThenInclude(x => x.ArtistResponse)
                .Include(x => x.TattooRequests)
                .ThenInclude(x => x.Consultation)
                .FirstOrDefaultAsync(i => i.Id == Id);
            if (artistFromData is null)
            {
                return null;
            }
            var result = new GetTattooArtistDto
            {
                FirstName = artistFromData.FirstName,
                LastName = artistFromData.LastName,
                DepositAmount = artistFromData.DepositAmount,
                Description = artistFromData.Description,
                Email = artistFromData.Email,
                OffersOnlineConsultation = artistFromData.OffersOnlineConsultation,
                PhoneNumber = artistFromData.PhoneNumber,
                RequiresDeposit = artistFromData.RequiresDeposit,
                StudioAddress = artistFromData.StudioAddress,
                StudioName = artistFromData.StudioName,
                Schedules = artistFromData.Schedules.Select(x => new TattooArtistScheduleDto
                {
                    DayOfWeek = x.DayOfWeek,
                    EndTime = x.EndTime,
                    StartTime = x.StartTime,
                }).ToList(),
                PortfolioImages = artistFromData.PortfolioImages.Select(x => new TattooArtistPortfolioImageDto
                {
                    ImageUrl = x.ImageUrl
                }).ToList(),
                Requirements = artistFromData.Requirements.Select(x => new TattooArtistRequirementsDto
                {
                    Description = x.Description
                }).ToList(),
                TattooRequests = artistFromData.TattooRequests == null || !artistFromData.TattooRequests.Any()? null
                : artistFromData.TattooRequests.Select(x => new TattooRequestDto
                {
                    Status = x.Status,
                    CreatedOn = x.CreatedOn,
                    Description = x.Description,
                    Placement = x.Placement,
                    ClientId = x.ClientId,
                    TattooArtistId = x.TattooArtistId,
                    Images = x.Images.Select(f => new TattooReferenceImageDto
                    {
                        ImageUrl = f.ImageUrl
                    }).ToList(),
                    TattooSessions = x.TattooSessions == null || !x.TattooSessions.Any()? null
                    : x.TattooSessions.Select(f => new TattooSessionDto
                    {
                        StartTime = f.StartTime,
                        DurationHours = f.DurationHours,
                        EndTime = f.EndTime,
                        PriceForTheSession = f.PriceForTheSession
                    }).ToList(),
                    ArtistResponse = x.ArtistResponse == null ? null
                    : new ArtistResponseDto
                    {
                        CreatedOn = x.ArtistResponse.CreatedOn,
                        EstimatedHours = x.ArtistResponse.EstimatedHours,
                        EstimatedPrice = x.ArtistResponse.EstimatedPrice,
                        ResponseMessage = x.ArtistResponse.ResponseMessage
                    },
                    Consultation = x.Consultation == null ? null
                    : new ConsultationDto
                    {
                        StartTime = x.Consultation.StartTime,
                        EndTime = x.Consultation.EndTime,
                        IsOnline = x.Consultation.IsOnline,
                        Notes = x.Consultation.Notes
                    }
                }).ToList()
            };
            return result;
        }

        public async Task<bool> CreateTattooArtistProfileAsync(
            CreateTattooArtistDto dto,
            string userId)
            {
            var alreadyHasArtistProfile = await context.TattooArtists
                .AnyAsync(a => a.UserId == userId);

            if (alreadyHasArtistProfile)
            {
                return false;
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return false;
            }

            if (dto.RequiresDeposit &&
                (dto.DepositAmount == null || dto.DepositAmount <= 0))
            {
                return false;
            }

            if (!await roleManager.RoleExistsAsync(UserRoles.Client))
            {
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Client));
            }

            if (!await roleManager.RoleExistsAsync(UserRoles.TattooArtist))
            {
                await roleManager.CreateAsync(new IdentityRole(UserRoles.TattooArtist));
            }

            if (!await userManager.IsInRoleAsync(user, UserRoles.Client))
            {
                await userManager.AddToRoleAsync(user, UserRoles.Client);
            }

            if (!await userManager.IsInRoleAsync(user, UserRoles.TattooArtist))
            {
                await userManager.AddToRoleAsync(user, UserRoles.TattooArtist);
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
                    UserId = user.Id
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
                PhoneNumber = dto.PhoneNumber,

                IsVerified = false,

                OffersOnlineConsultation = dto.OffersOnlineConsultation,
                RequiresDeposit = dto.RequiresDeposit,
                DepositAmount = dto.RequiresDeposit ? dto.DepositAmount : null,

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
                    EndTime = s.EndTime
                }).ToList()
            };

            context.TattooArtists.Add(tattooArtist);

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteArtist(int id)
        {
            var artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.Id == id);
            if (artist is null)
            {
                
                return false;
            }
            context.TattooArtists.Remove(artist);

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateArtist(int id, UpdateArtistDto dto)
        {
            TattooArtist artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.Id == id);
            if (artist == null)
            {
                return false;
            }
            
                artist.FirstName = dto.FirstName;
                artist.LastName = dto.LastName;
                artist.DepositAmount = dto.DepositAmount;
                artist.Description = dto.Description;
                artist.Email = dto.Email;
                artist.OffersOnlineConsultation = dto.OffersOnlineConsultation;
                artist.PhoneNumber = dto.PhoneNumber;
                artist.RequiresDeposit = dto.RequiresDeposit;
                artist.StudioAddress = dto.StudioAddress;
                artist.StudioName = dto.StudioName;

            await context.SaveChangesAsync();
            return true;
            
        }

        private async Task EnsureRoleExists(string role)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
