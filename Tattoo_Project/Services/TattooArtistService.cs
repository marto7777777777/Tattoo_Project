using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.TattooArtistDTOs;
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
        public async Task<ICollection<GetTattooArtistDto>> GetAllTattooArtistsAsync()
        {
            return await context.TattooArtists
                .Select(a => new GetTattooArtistDto
                {
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Email = a.Email,
                    StudioName = a.StudioName,
                    Description = a.Description,
                    StudioAddress = a.StudioAddress,
                    PhoneNumber = a.PhoneNumber,
                    IsVerified = a.IsVerified,
                    OffersOnlineConsultation = a.OffersOnlineConsultation,
                    RequiresDeposit = a.RequiresDeposit,
                    DepositAmount = a.DepositAmount,
                    Requirements = a.Requirements.Select(r => new TattooArtistRequirementsDto
                    {
                        Description = r.Description
                    }).ToList(),
                    PortfolioImages = a.PortfolioImages.Select(p => new TattooArtistPortfolioImageDto
                    {
                        ImageUrl = p.ImageUrl,
                    }).ToList(),
                    Schedules = a.Schedules.Select(s => new TattooArtistScheduleDto
                    {
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<GetTattooArtistDto?> GetTattooArtistByIdAsync(int id)
        {
            var tattooArtist = await context.TattooArtists
                .Include(a => a.Requirements)
                .Include(a => a.PortfolioImages)
                .Include(a => a.Schedules)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (tattooArtist == null)
            {
                return null;
            }

            return new GetTattooArtistDto
            {
                FirstName = tattooArtist.FirstName,
                LastName = tattooArtist.LastName,
                Email = tattooArtist.Email,
                StudioName = tattooArtist.StudioName,
                Description = tattooArtist.Description,
                StudioAddress = tattooArtist.StudioAddress,
                PhoneNumber = tattooArtist.PhoneNumber,
                IsVerified = tattooArtist.IsVerified,
                OffersOnlineConsultation = tattooArtist.OffersOnlineConsultation,
                RequiresDeposit = tattooArtist.RequiresDeposit,
                DepositAmount = tattooArtist.DepositAmount,
                Requirements = tattooArtist.Requirements.Select(r => new TattooArtistRequirementsDto
                {
                    Description = r.Description
                }).ToList(),
                PortfolioImages = tattooArtist.PortfolioImages.Select(p => new TattooArtistPortfolioImageDto
                {
                    ImageUrl = p.ImageUrl
                }).ToList(),
                Schedules = tattooArtist.Schedules.Select(s => new TattooArtistScheduleDto
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToList()
            };
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

            if (!dto.RequiresDeposit && dto.DepositAmount != null)
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

        public async Task<bool> UpdateTattooArtistProfileAsync(
            UpdateArtistDto dto,
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .Include(a => a.Requirements)
                .Include(a => a.PortfolioImages)
                .Include(a => a.Schedules)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return false;
            }

            if (dto.RequiresDeposit &&
                (dto.DepositAmount == null || dto.DepositAmount <= 0))
            {
                return false;
            }

            if (!dto.RequiresDeposit && dto.DepositAmount != null)
            {
                return false;
            }

            tattooArtist.StudioName = dto.StudioName;
            tattooArtist.Description = dto.Description;
            tattooArtist.StudioAddress = dto.StudioAddress;
            tattooArtist.PhoneNumber = dto.PhoneNumber;
            tattooArtist.OffersOnlineConsultation = dto.OffersOnlineConsultation;
            tattooArtist.RequiresDeposit = dto.RequiresDeposit;
            tattooArtist.DepositAmount = dto.RequiresDeposit ? dto.DepositAmount : null;

            tattooArtist.Requirements.Clear();
            tattooArtist.PortfolioImages.Clear();
            tattooArtist.Schedules.Clear();

            foreach (var requirement in dto.Requirements)
            {
                tattooArtist.Requirements.Add(new ArtistRequirement
                {
                    Description = requirement.Description
                });
            }

            foreach (var image in dto.PortfolioImages)
            {
                tattooArtist.PortfolioImages.Add(new PortfolioImage
                {
                    ImageUrl = image.ImageUrl
                });
            }

            foreach (var schedule in dto.Schedules)
            {
                tattooArtist.Schedules.Add(new Schedule
                {
                    DayOfWeek = schedule.DayOfWeek,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime
                });
            }

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteTattooArtistAsync(int id)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.Id == id);

            if (tattooArtist == null)
            {
                return false;
            }

            context.TattooArtists.Remove(tattooArtist);

            await context.SaveChangesAsync();

            return true;
        }
    }
}