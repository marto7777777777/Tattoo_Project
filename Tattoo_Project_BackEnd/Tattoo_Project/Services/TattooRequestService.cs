using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ArtistResponceDTOs;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.DTOs.TattooReferenceImageDTOs;
using Tattoo_Project.DTOs.TattooRequestDTOs;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class TattooRequestService(TattooDbContext context)
        : ITattooRequestService
    {
        public async Task<ResultService<ICollection<GetTattooRequestDto>>> GetAllTattooRequestsAsync()
        {
            var tattooRequests = await context.TattooRequests
                .Include(r => r.Images)
                .Include(r => r.TattooSessions)
                .Include(r => r.ArtistResponse)
                .Include(r => r.Consultation)
                .ToListAsync();

            var result = tattooRequests
                .Select(MapToGetTattooRequestDto)
                .ToList();

            return ResultService<ICollection<GetTattooRequestDto>>.Ok(result);
        }

        public async Task<ResultService<GetTattooRequestDto>> GetTattooRequestByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist)
        {
            var tattooRequest = await context.TattooRequests
                .Include(r => r.Images)
                .Include(r => r.TattooSessions)
                .Include(r => r.ArtistResponse)
                .Include(r => r.Consultation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (tattooRequest == null)
            {
                return ResultService<GetTattooRequestDto>.Fail("Tattoo request was not found.");
            }

            if (isAdmin)
            {
                return ResultService<GetTattooRequestDto>.Ok(
                    MapToGetTattooRequestDto(tattooRequest));
            }

            if (isClient)
            {
                var client = await context.Clients
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (client != null &&
                    tattooRequest.ClientId == client.Id)
                {
                    return ResultService<GetTattooRequestDto>.Ok(
                        MapToGetTattooRequestDto(tattooRequest));
                }
            }

            if (isArtist)
            {
                var tattooArtist = await context.TattooArtists
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (tattooArtist != null &&
                    tattooRequest.TattooArtistId == tattooArtist.Id)
                {
                    return ResultService<GetTattooRequestDto>.Ok(
                        MapToGetTattooRequestDto(tattooRequest));
                }
            }

            return ResultService<GetTattooRequestDto>.Fail(
                "You do not have permission to view this tattoo request.");
        }

        public async Task<ResultService<ICollection<GetTattooRequestDto>>> GetMyTattooRequestsAsync(
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService<ICollection<GetTattooRequestDto>>.Fail(
                    "Client profile was not found.");
            }

            var tattooRequests = await context.TattooRequests
                .Include(r => r.Images)
                .Include(r => r.TattooSessions)
                .Include(r => r.ArtistResponse)
                .Include(r => r.Consultation)
                .Where(r => r.ClientId == client.Id)
                .ToListAsync();

            var result = tattooRequests
                .Select(MapToGetTattooRequestDto)
                .ToList();

            return ResultService<ICollection<GetTattooRequestDto>>.Ok(result);
        }

        public async Task<ResultService> CreateTattooRequestAsync(
            CreateTattooRequestDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile was not found.");
            }

            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.Id == dto.TattooArtistId);

            if (tattooArtist == null)
            {
                return ResultService.Fail("Tattoo artist was not found.");
            }

            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                return ResultService.Fail("Tattoo request description is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Placement))
            {
                return ResultService.Fail("Tattoo placement is required.");
            }

            if (dto.Images.Any(i => string.IsNullOrWhiteSpace(i.ImageUrl)))
            {
                return ResultService.Fail("Every reference image must have a valid image URL.");
            }

            TattooRequest tattooRequest = new()
            {
                ClientId = client.Id,
                TattooArtistId = tattooArtist.Id,
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

            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateTattooRequestAsync(
            int id,
            UpdateTattooRequestDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile was not found.");
            }

            var tattooRequest = await context.TattooRequests
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (tattooRequest == null)
            {
                return ResultService.Fail("Tattoo request was not found.");
            }

            if (tattooRequest.ClientId != client.Id)
            {
                return ResultService.Fail("You can update only your own tattoo requests.");
            }

            if (tattooRequest.Status != RequestStatus.Submitted)
            {
                return ResultService.Fail(
                    "Tattoo request can be updated only while it is submitted.");
            }

            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                return ResultService.Fail("Tattoo request description is required.");
            }

            if (dto.Images.Any(i => string.IsNullOrWhiteSpace(i.ImageUrl)))
            {
                return ResultService.Fail("Every reference image must have a valid image URL.");
            }

            tattooRequest.Description = dto.Description;

            tattooRequest.Images.Clear();

            foreach (var image in dto.Images)
            {
                tattooRequest.Images.Add(new TattooReferenceImage
                {
                    ImageUrl = image.ImageUrl
                });
            }

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        private static GetTattooRequestDto MapToGetTattooRequestDto(
            TattooRequest tattooRequest)
        {
            return new GetTattooRequestDto
            {
                Id = tattooRequest.Id,
                Description = tattooRequest.Description,
                Placement = tattooRequest.Placement,
                CreatedOn = tattooRequest.CreatedOn,
                ClientId = tattooRequest.ClientId,
                TattooArtistId = tattooRequest.TattooArtistId,
                Status = tattooRequest.Status,

                Images = tattooRequest.Images.Select(i => new TattooReferenceImageDto
                {
                    ImageUrl = i.ImageUrl
                }).ToList(),

                TattooSessions = tattooRequest.TattooSessions == null ||
                                 !tattooRequest.TattooSessions.Any()
                    ? null
                    : tattooRequest.TattooSessions.Select(s => new TattooSessionDto
                    {
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        DurationHours = s.DurationHours,
                        PriceForTheSession = s.PriceForTheSession
                    }).ToList(),

                ArtistResponse = tattooRequest.ArtistResponse == null
                    ? null
                    : new ArtistResponseDto
                    {
                        EstimatedPrice = tattooRequest.ArtistResponse.EstimatedPrice,
                        EstimatedHours = tattooRequest.ArtistResponse.EstimatedHours,
                        ResponseMessage = tattooRequest.ArtistResponse.ResponseMessage,
                        CreatedOn = tattooRequest.ArtistResponse.CreatedOn
                    },

                Consultation = tattooRequest.Consultation == null
                    ? null
                    : new ConsultationDto
                    {
                        StartTime = tattooRequest.Consultation.StartTime,
                        EndTime = tattooRequest.Consultation.EndTime,
                        Notes = tattooRequest.Consultation.Notes
                    }
            };
        }
    }
}