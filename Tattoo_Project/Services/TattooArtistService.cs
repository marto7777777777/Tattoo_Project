using Tattoo_Project.Data;
using Tattoo_Project.Models;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.DTOs;

namespace Tattoo_Project.Services
{
    public class TattooArtistService(TattooDbContext context) : ITattooArtistService
    {
        public async Task<List<TattooArtistDto>> GetAllArtistsAsync()
        => await context.TattooArtists.Select(c => new TattooArtistDto
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





        public async Task<TattooArtistDto> GetTattooArtistByIdAsync(int Id)
        {
            var artistFromData = context.TattooArtists.FirstOrDefault(i => i.Id == Id);
            if (artistFromData is null)
            {
                return null;
            }
            var result = new TattooArtistDto
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
    }
}
