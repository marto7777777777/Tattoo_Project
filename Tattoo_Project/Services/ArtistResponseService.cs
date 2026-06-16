using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ArtistResponceDTOs;
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

        public async Task<ICollection<GetArtistResponseDto>> GetAllArtistResponsesAsync()
        {
            return await context.ArtistResponses
                .Select(r => new GetArtistResponseDto
                {
                    TattooRequestId = r.TattooRequestId,
                    ResponseMessage = r.ResponseMessage,
                    EstimatedPrice = r.EstimatedPrice,
                    CreatedOn = r.CreatedOn
                })
                .ToListAsync();
        }

        public async Task<GetArtistResponseDto?> GetArtistResponseByIdAsync(
    int id,
    string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return null;
            }

            var response = await context.ArtistResponses
                .Include(r => r.TattooRequest)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (response == null)
            {
                return null;
            }

            if (response.TattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return null;
            }

            return new GetArtistResponseDto
            {
                TattooRequestId = response.TattooRequestId,
                ResponseMessage = response.ResponseMessage,
                EstimatedPrice = response.EstimatedPrice,
                CreatedOn = response.CreatedOn
            };
        }

        public async Task<ICollection<GetArtistResponseDto>> GetMyArtistResponsesAsync(
    string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return new List<GetArtistResponseDto>();
            }

            return await context.ArtistResponses
                .Where(r => r.TattooRequest.TattooArtistId == tattooArtist.Id)
                .Select(r => new GetArtistResponseDto
                {
                    TattooRequestId = r.TattooRequestId,
                    ResponseMessage = r.ResponseMessage,
                    EstimatedPrice = r.EstimatedPrice,
                    CreatedOn = r.CreatedOn
                })
                .ToListAsync();
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
