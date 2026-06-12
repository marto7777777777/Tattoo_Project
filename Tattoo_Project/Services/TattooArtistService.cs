using Tattoo_Project.Data;
using Tattoo_Project.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Tattoo_Project.DTOs.TattooArtistDTOs;
using Tattoo_Project.DTOs.TattooRequestDTOs;
using Tattoo_Project.DTOs.TattooReferenceImageDTOs;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.DTOs.ArtistResponceDTOs;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class TattooArtistService(TattooDbContext context) : ITattooArtistService
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
                Schedules = dto.Schedules.Select(x => new Schedule
                {
                        StartTime = x.StartTime,
                        DayOfWeek = x.DayOfWeek,
                        EndTime = x.EndTime
                }).ToList(),
                Requirements = dto.Requirements.Select(x => new ArtistRequirement
                {
                    Description = x.Description
                }).ToList(),
                PortfolioImages = dto.PortfolioImages.Select(x => new PortfolioImage
                {
                    ImageUrl = x.ImageUrl
                }).ToList(),
            };
            context.TattooArtists.Add(artist);
            await context.SaveChangesAsync();
            return artist.Id;
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
    }
}
