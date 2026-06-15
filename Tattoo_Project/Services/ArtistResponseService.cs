using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ArtistResponseDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class ArtistResponseService(TattooDbContext context) : IArtistResponseService
    {
        public async Task<bool> CreateArtistResponseAsync(
    CreateArtistResponseDto dto,
    string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return false;
            }

            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == dto.TattooRequestId);

            if (tattooRequest == null)
            {
                return false;
            }

            if (tattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return false;
            }

            var alreadyHasResponse = await context.ArtistResponses
                .AnyAsync(r => r.TattooRequestId == dto.TattooRequestId);

            if (alreadyHasResponse)
            {
                return false;
            }

            if (tattooRequest.Status != RequestStatus.Submitted)
            {
                return false;
            }

            ArtistResponse artistResponse = new()
            {
                TattooRequestId = dto.TattooRequestId,
                ResponseMessage = dto.ResponseMessage,
                EstimatedPrice = dto.EstimatedPrice,
                CreatedOn = DateTime.UtcNow,
                EstimatedHours = dto.EstimatedHours
            };

            tattooRequest.Status = RequestStatus.WaitingForConsultation;

            context.ArtistResponses.Add(artistResponse);

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteArtistResponseAsync(int id)
        {
            var response = await context.ArtistResponses.FirstOrDefaultAsync(x => x.Id == id);
            if (response == null)
            {
                return false;
            }
            context.Remove(response);
            await context.SaveChangesAsync();
            return true;

        }

        public async Task<List<GetArtistResponseDto>> GetAllArtistResponsesAsync()
        {
            if (context.ArtistResponses == null || !context.ArtistResponses.Any())
            {
                return null;
            }
            return await context.ArtistResponses.Select(x => new GetArtistResponseDto
                {
                    CreatedOn = x.CreatedOn,
                    EstimatedHours = x.EstimatedHours,
                    EstimatedPrice = x.EstimatedPrice,
                    ResponseMessage = x.ResponseMessage,
                    TattooRequestId = x.TattooRequestId
                }).ToListAsync();
        }

        public async Task<GetArtistResponseDto> GetArtistResponseByIdAsync(int id)
        {
            var artistResponse = await context.ArtistResponses.FirstOrDefaultAsync(x => x.Id == id);
            if (artistResponse == null)
            {
                return null;
            }
            var artistResponseDto = new GetArtistResponseDto
            {
                CreatedOn = artistResponse.CreatedOn,
                EstimatedHours = artistResponse.EstimatedHours,
                EstimatedPrice = artistResponse.EstimatedPrice,
                ResponseMessage = artistResponse.ResponseMessage,
                TattooRequestId = artistResponse.TattooRequestId
            };
            return artistResponseDto;
        }

        public async Task<bool> RejectTattooRequestAsync(
    int tattooRequestId,
    string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return false;
            }

            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);

            if (tattooRequest == null)
            {
                return false;
            }

            if (tattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return false;
            }

            if (tattooRequest.Status != RequestStatus.Submitted)
            {
                return false;
            }

            var alreadyHasResponse = await context.ArtistResponses
                .AnyAsync(r => r.TattooRequestId == tattooRequestId);

            if (alreadyHasResponse)
            {
                return false;
            }

            tattooRequest.Status = RequestStatus.Rejected;

            await context.SaveChangesAsync();

            return true;
        }
    }
}
