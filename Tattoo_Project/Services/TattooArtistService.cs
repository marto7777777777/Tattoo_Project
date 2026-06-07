using Tattoo_Project.Data;
using Tattoo_Project.Models;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Tattoo_Project.Services
{
    public class TattooArtistService(TattooDbContext context) : ITattooArtistService
    {
        public async Task<List<GetTattooArtistDto>> GetAllArtistsAsync()
        => await context.TattooArtists.Select(c => new GetTattooArtistDto
        {
            FirstName = c.FirstName,
            LastName = c.LastName,
            DepositAmount = c.DepositAmount,
            Description = c.Description,
            Email = c.Email,
            OffersOnlineConsultation = c.OffersOnlineConsultation,
            PhoneNumber = c.PhoneNumber,
            PortfolioImages = c.PortfolioImages,
            Requirements = c.Requirements,
            RequiresDeposit = c.RequiresDeposit,
            Schedules = c.Schedules,
            StudioAddress = c.StudioAddress,
            StudioName = c.StudioName,
            TattooRequests = c.TattooRequests
        }).ToListAsync();





        public async Task<GetTattooArtistDto> GetTattooArtistByIdAsync(int Id)
        {
            var artistFromData = context.TattooArtists.FirstOrDefault(i => i.Id == Id);
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
                PortfolioImages = artistFromData.PortfolioImages,
                Requirements = artistFromData.Requirements,
                RequiresDeposit = artistFromData.RequiresDeposit,
                Schedules = artistFromData.Schedules,
                StudioAddress = artistFromData.StudioAddress,
                StudioName = artistFromData.StudioName,
                TattooRequests = artistFromData.TattooRequests
            };

            return await Task.FromResult(result);
        }

        public async Task<int> CreateArtist(CreateTattooArtistDto dto)
        {
            TattooArtist artist = new TattooArtist()
            {
                FirstName = dto.FirstName,
                LastName= dto.LastName,
                DepositAmount = dto.DepositAmount,
                Description = dto.Description,
                Email = dto.Email,
                OffersOnlineConsultation = dto.OffersOnlineConsultation,
                PhoneNumber = dto.PhoneNumber,
                RequiresDeposit = dto.RequiresDeposit,
                StudioAddress = dto.StudioAddress,
                StudioName = dto.StudioName,
            };

            foreach (var schedule in dto.Schedules)
            {
                artist.Schedules.Add(new Schedule()
                {
                    DayOfWeek = schedule.DayOfWeek,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime
                });
            }
            foreach(var portfolioImage in dto.PortfolioImages)
            {
                artist.PortfolioImages.Add(new PortfolioImage()
                {
                    ImageUrl = portfolioImage.ImageUrl
                });
            }

            foreach (var requirement in dto.Requirements)
            {
                artist.Requirements.Add(new ArtistRequirement()
                {
                    Description = requirement.Description
                });
            }
            context.TattooArtists.Add(artist);
            await context.SaveChangesAsync();
            return artist.Id;
        }

        public async Task<bool> DeleteArtist(int id)
        {
            var artist = context.TattooArtists.FirstOrDefault(x => x.Id == id);
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
    }
}
