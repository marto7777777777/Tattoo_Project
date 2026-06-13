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
        public async Task<bool> CreateArtistResponseAsync(CreateArtistResponseDto dto)
        {
            var tattooRequest = await context.TattooRequests
                .Include(x => x.ArtistResponse)
                .FirstOrDefaultAsync(x => x.Id == dto.TattooRequestId);

            if (tattooRequest == null)
            {
                return false; //Tattoo request not found.
            }

            if (tattooRequest.ArtistResponse != null)
            {
                return false; //This request already has a response.
            }
            await context.ArtistResponses.AddAsync(new Models.ArtistResponse
            {
                CreatedOn = DateTime.Now,
                EstimatedHours = dto.EstimatedHours,
                EstimatedPrice = dto.EstimatedPrice,
                ResponseMessage = dto.ResponseMessage,
                TattooRequestId = dto.TattooRequestId
            });

            tattooRequest.Status = Models.RequestStatus.WaitingForConsultation;

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

        public async Task<bool> RejectTattooRequestAsync(int tattooRequestId)
        {
            var request = await context.TattooRequests.FirstOrDefaultAsync(x => x.Id == tattooRequestId);

            if (request is null)
            {
                return false;
            }

            //Проверка за статуса
            if (request.Status != RequestStatus.Submitted &&
                request.Status != RequestStatus.UnderReview)
            {
                return false;
            }

            request.Status = RequestStatus.Rejected;

            await context.SaveChangesAsync();

            return true;
        }
    }
}
