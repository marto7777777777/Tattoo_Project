using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ArtistResponceDTOs;
using Tattoo_Project.DTOs.ArtistResponseDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class ArtistResponseService(TattooDbContext context) : IArtistResponseService
    {
        public async Task<ResultService<ICollection<GetArtistResponseDto>>> GetAllArtistResponsesAsync()
        {
            var responses = await context.ArtistResponses
                .Select(r => new GetArtistResponseDto
                {
                    TattooRequestId = r.TattooRequestId,
                    ResponseMessage = r.ResponseMessage,
                    EstimatedPrice = r.EstimatedPrice,
                    CreatedOn = r.CreatedOn
                })
                .ToListAsync();

            return ResultService<ICollection<GetArtistResponseDto>>.Ok(responses);
        }

        public async Task<ResultService<GetArtistResponseDto>> GetArtistResponseByIdAsync(
            int id,
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService<GetArtistResponseDto>.Fail(
                    "Tattoo artist profile was not found.");
            }

            var response = await context.ArtistResponses
                .Include(r => r.TattooRequest)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (response == null)
            {
                return ResultService<GetArtistResponseDto>.Fail(
                    "Artist response was not found.");
            }

            if (response.TattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return ResultService<GetArtistResponseDto>.Fail(
                    "You can view only your own artist responses.");
            }

            var dto = new GetArtistResponseDto
            {
                TattooRequestId = response.TattooRequestId,
                ResponseMessage = response.ResponseMessage,
                EstimatedPrice = response.EstimatedPrice,
                CreatedOn = response.CreatedOn
            };

            return ResultService<GetArtistResponseDto>.Ok(dto);
        }

        public async Task<ResultService<ICollection<GetArtistResponseDto>>> GetMyArtistResponsesAsync(
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService<ICollection<GetArtistResponseDto>>.Fail(
                    "Tattoo artist profile was not found.");
            }

            var responses = await context.ArtistResponses
                .Where(r => r.TattooRequest.TattooArtistId == tattooArtist.Id)
                .Select(r => new GetArtistResponseDto
                {
                    TattooRequestId = r.TattooRequestId,
                    ResponseMessage = r.ResponseMessage,
                    EstimatedPrice = r.EstimatedPrice,
                    CreatedOn = r.CreatedOn
                })
                .ToListAsync();

            return ResultService<ICollection<GetArtistResponseDto>>.Ok(responses);
        }

        public async Task<ResultService> CreateArtistResponseAsync(
            CreateArtistResponseDto dto,
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == dto.TattooRequestId);

            if (tattooRequest == null)
            {
                return ResultService.Fail("Tattoo request was not found.");
            }

            if (tattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return ResultService.Fail(
                    "You can respond only to tattoo requests assigned to you.");
            }

            if (tattooRequest.Status != RequestStatus.Submitted)
            {
                return ResultService.Fail(
                    "Artist response can be created only for submitted tattoo requests.");
            }

            var alreadyHasResponse = await context.ArtistResponses
                .AnyAsync(r => r.TattooRequestId == dto.TattooRequestId);

            if (alreadyHasResponse)
            {
                return ResultService.Fail(
                    "This tattoo request already has an artist response.");
            }

            ArtistResponse artistResponse = new()
            {
                TattooRequestId = dto.TattooRequestId,
                ResponseMessage = dto.ResponseMessage,
                EstimatedPrice = dto.EstimatedPrice,
                CreatedOn = DateTime.UtcNow
            };

            tattooRequest.Status = RequestStatus.WaitingForConsultation;

            context.ArtistResponses.Add(artistResponse);

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> RejectTattooRequestAsync(
            int tattooRequestId,
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist profile was not found.");
            }

            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);

            if (tattooRequest == null)
            {
                return ResultService.Fail("Tattoo request was not found.");
            }

            if (tattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return ResultService.Fail(
                    "You can reject only tattoo requests assigned to you.");
            }

            if (tattooRequest.Status != RequestStatus.Submitted)
            {
                return ResultService.Fail(
                    "Only submitted tattoo requests can be rejected before artist response.");
            }

            var alreadyHasResponse = await context.ArtistResponses
                .AnyAsync(r => r.TattooRequestId == tattooRequestId);

            if (alreadyHasResponse)
            {
                return ResultService.Fail(
                    "Tattoo request already has an artist response.");
            }

            tattooRequest.Status = RequestStatus.Rejected;

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }
    }
}
