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
    public class TattooRequestService(TattooDbContext context) : ITattooRequestService
    {
        public async Task<List<GetTattooRequestDto>> GetAllTattooRequestsAsync()
        {
            var tattooRequests = await context.TattooRequests.Select(x => new GetTattooRequestDto
            {
                Description = x.Description,
                Status = x.Status,
                CreatedOn = x.CreatedOn,
                Placement = x.Placement,
                ClientId = x.ClientId,
                TattooArtistId = x.TattooArtistId,
                ArtistResponse = x.ArtistResponse == null ? null
                : new ArtistResponseDto
                {
                    EstimatedPrice = x.ArtistResponse.EstimatedPrice,
                    EstimatedHours = x.ArtistResponse.EstimatedHours,
                    CreatedOn = x.ArtistResponse.CreatedOn,
                    ResponseMessage = x.ArtistResponse.ResponseMessage
                },
                Consultation = x.Consultation == null ? null
                : new ConsultationDto
                {
                    StartTime = x.Consultation.StartTime,
                    EndTime = x.Consultation.EndTime,
                    IsOnline = x.Consultation.IsOnline,
                    Notes = x.Consultation.Notes
                },
                TattooSessions = x.TattooSessions == null || !x.TattooSessions.Any() ? null
                : x.TattooSessions.Select(x => new TattooSessionDto
                {
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    DurationHours = x.DurationHours,
                    PriceForTheSession = x.PriceForTheSession
                }).ToList(),
                Images = x.Images.Select(x => new TattooReferenceImageDto
                {
                    ImageUrl = x.ImageUrl
                }).ToList()
            }).ToListAsync();
            return tattooRequests;
        }


        public async Task<bool> CreateTattooRequest(
            CreateTattooRequestDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return false;
            }

            var tattooArtistExists = await context.TattooArtists
                .AnyAsync(a => a.Id == dto.TattooArtistId);

            if (!tattooArtistExists)
            {
                return false;
            }

            TattooRequest tattooRequest = new()
            {
                ClientId = client.Id,
                TattooArtistId = dto.TattooArtistId,
                Description = dto.Description,
                Placement = dto.Placement,
                Status = RequestStatus.Submitted,
                CreatedOn = DateTime.UtcNow,

                Images = dto.Images.Select(i => new TattooReferenceImage
                {
                    ImageUrl = i.ImageUrl
                }).ToList()
            };

            context.TattooRequests.Add(tattooRequest);

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteTattooRequest(int id)
        {
            throw new NotImplementedException();
        }


        public async Task<GetTattooRequestDto> GetTattooRequestByIdAsync(int id)
        {
            var tattooRequest = await context.TattooRequests
                .Include(x => x.ArtistResponse)
                .Include(x => x.Consultation)
                .Include(x => x.TattooSessions)
                .Include(x => x.Consultation)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (tattooRequest == null)
            {
                return null;
            }
            var tattooRequestDto = new GetTattooRequestDto
            {
                Description = tattooRequest.Description,
                Status = tattooRequest.Status,
                CreatedOn = tattooRequest.CreatedOn,
                Placement = tattooRequest.Placement,
                ClientId = tattooRequest.ClientId,
                TattooArtistId = tattooRequest.TattooArtistId,
                ArtistResponse = tattooRequest.ArtistResponse == null ? null
                : new ArtistResponseDto
                {
                    EstimatedPrice = tattooRequest.ArtistResponse.EstimatedPrice,
                    EstimatedHours = tattooRequest.ArtistResponse.EstimatedHours,
                    CreatedOn = tattooRequest.ArtistResponse.CreatedOn,
                    ResponseMessage = tattooRequest.ArtistResponse.ResponseMessage
                },
                Consultation = tattooRequest.Consultation == null ? null
                : new ConsultationDto
                {
                    StartTime = tattooRequest.Consultation.StartTime,
                    EndTime = tattooRequest.Consultation.EndTime,
                    IsOnline = tattooRequest.Consultation.IsOnline,
                    Notes = tattooRequest.Consultation.Notes
                },
                TattooSessions = tattooRequest.TattooSessions == null || !tattooRequest.TattooSessions.Any() ? null
                : tattooRequest.TattooSessions.Select(x => new TattooSessionDto
                {
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    DurationHours = x.DurationHours,
                    PriceForTheSession = x.PriceForTheSession
                }).ToList(),
                Images = tattooRequest.Images.Select(x => new TattooReferenceImageDto
                {
                    ImageUrl = x.ImageUrl
                }).ToList()
            };
            return tattooRequestDto;
        }


        public async Task<bool> UpdateTattooRequest(int id, UpdateTattooRequestDto dto)
        {
            var tattooRequest = await context.TattooRequests
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (tattooRequest == null)
            {
                return false;
            }

            tattooRequest.Images = dto.Images.Select(x => new TattooReferenceImage
            {
                ImageUrl = x.ImageUrl
            }).ToList();
            tattooRequest.Description = dto.Description;
            await context.SaveChangesAsync();
            return true;
        }
    }
}
